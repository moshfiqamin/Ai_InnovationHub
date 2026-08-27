// ============================================================
// MODULE : M14 — Administration
// LAYER  : Service
// FEATURE: F20 — Admin & AI Content Moderation
// PURPOSE: Screens user content with the AI, records reports, and
//          exposes the admin review queue.
// DESIGN : AI moderation NEVER deletes content on its own. It only
//          flags and opens a report for a human administrator, which
//          keeps a person in the loop for every removal (NFR15).
// ============================================================
using System.Text.Json;
using AiInnovationHub.Api.Data;
using AiInnovationHub.Api.Models.DTOs;
using AiInnovationHub.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiInnovationHub.Api.Services;

public interface IModerationService
{
    Task ScreenPostAsync(Guid postId, CancellationToken ct = default);
    Task<(bool ok, string error)> ReportAsync(Guid reporterId, ReportRequest req, CancellationToken ct = default);
    Task<List<ReportDto>> QueueAsync(string? status, CancellationToken ct = default);
    Task<bool> ResolveAsync(Guid reportId, Guid adminId, string action, CancellationToken ct = default);
}

public class ModerationService : IModerationService
{
    private readonly AppDbContext _db;
    private readonly IAiProvider _ai;
    private readonly ILogger<ModerationService> _log;

    public ModerationService(AppDbContext db, IAiProvider ai, ILogger<ModerationService> log)
    {
        _db = db; _ai = ai; _log = log;
    }

    // ==========================================================
    // F20 — AI SCREENING OF A NEW POST
    // Runs after the post is saved. If the model is unavailable the
    // post simply stays unflagged rather than blocking publication.
    // ==========================================================
    public async Task ScreenPostAsync(Guid postId, CancellationToken ct = default)
    {
        var post = await _db.CommunityPosts.FirstOrDefaultAsync(p => p.Id == postId, ct);
        if (post is null) return;

        var prompt = $$"""
        You are a content moderation assistant for an innovation platform.
        Classify the following community post.

        Title  : {{post.Title}}
        Content: {{post.Content}}

        Reply with ONLY a JSON object, no markdown fences:
        {"verdict":"Safe|Review|Unsafe","reason":"one short sentence"}

        Use "Safe" for ordinary discussion. Use "Review" for borderline
        content such as spam, self-promotion or mild hostility. Use
        "Unsafe" only for harassment, hate, explicit content or clear
        illegality.
        """;

        var raw = await _ai.GenerateTextAsync(prompt, ct);
        if (string.IsNullOrWhiteSpace(raw))
        {
            _log.LogInformation("AI moderation unavailable — post {Id} left unflagged.", postId);
            return;
        }

        try
        {
            var json = AiJson.Slice(raw);
            if (json is null) return;
            using var doc = JsonDocument.Parse(json);
            var verdict = doc.RootElement.GetProperty("verdict").GetString() ?? "Safe";
            var reason = doc.RootElement.TryGetProperty("reason", out var r) ? r.GetString() : null;

            // Only non-Safe verdicts create work for an administrator.
            if (verdict is "Review" or "Unsafe")
            {
                post.IsFlagged = true;
                _db.ContentReports.Add(new ContentReport
                {
                    TargetType = "Post", TargetId = post.Id,
                    TargetPreview = Format.Truncate($"{post.Title} — {post.Content}", 200),
                    Reason = "Flagged automatically by AI moderation",
                    ReporterId = null,            // null = raised by the AI, not a person
                    AiVerdict = verdict, AiReason = reason, AiCheckedAt = DateTime.UtcNow,
                });
                await _db.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not parse AI moderation verdict for post {Id}", postId);
        }
    }

    // ==========================================================
    // F20 — A USER REPORTS CONTENT
    // ==========================================================
    public async Task<(bool, string)> ReportAsync(Guid reporterId, ReportRequest req, CancellationToken ct = default)
    {
        // Resolve a readable preview so admins can judge without
        // following the link, and to confirm the target really exists.
        string preview = req.TargetType switch
        {
            "Idea"    => (await _db.Ideas.FirstOrDefaultAsync(i => i.Id == req.TargetId, ct))?.Title ?? "",
            "Post"    => (await _db.CommunityPosts.FirstOrDefaultAsync(p => p.Id == req.TargetId, ct))?.Title ?? "",
            "Comment" => (await _db.Comments.FirstOrDefaultAsync(c => c.Id == req.TargetId, ct))?.Content ?? "",
            _ => "",
        };
        if (string.IsNullOrEmpty(preview)) return (false, "That content no longer exists.");

        // One open report per user per item keeps the queue clean.
        var duplicate = await _db.ContentReports.AnyAsync(
            r => r.TargetId == req.TargetId && r.ReporterId == reporterId && r.Status == "Pending", ct);
        if (duplicate) return (false, "You have already reported this, and it is awaiting review.");

        _db.ContentReports.Add(new ContentReport
        {
            TargetType = req.TargetType, TargetId = req.TargetId,
            TargetPreview = Format.Truncate(preview, 200),
            Reason = req.Reason.Trim(), ReporterId = reporterId,
        });
        await _db.SaveChangesAsync(ct);
        return (true, "");
    }

    // ---- The admin review queue ----
    public async Task<List<ReportDto>> QueueAsync(string? status, CancellationToken ct = default)
    {
        var q = _db.ContentReports.Include(r => r.Reporter).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && status != "All")
            q = q.Where(r => r.Status == status);

        var rows = await q.OrderByDescending(r => r.CreatedAt).Take(100).ToListAsync(ct);

        return rows.Select(r => new ReportDto
        {
            Id = r.Id, TargetType = r.TargetType, TargetId = r.TargetId,
            TargetPreview = r.TargetPreview, Reason = r.Reason, Status = r.Status,
            // "AI moderation" rather than a name when nobody reported it.
            ReporterName = r.Reporter?.FullName ?? "AI moderation",
            AiVerdict = r.AiVerdict, AiReason = r.AiReason,
            TimeAgo = Format.TimeAgo(r.CreatedAt),
        }).ToList();
    }

    // ==========================================================
    // F20 — ADMIN RESOLVES A REPORT
    // action: "dismiss" keeps the content, "remove" deletes it.
    // ==========================================================
    public async Task<bool> ResolveAsync(Guid reportId, Guid adminId, string action, CancellationToken ct = default)
    {
        var report = await _db.ContentReports.FirstOrDefaultAsync(r => r.Id == reportId, ct);
        if (report is null) return false;

        if (action == "remove")
        {
            switch (report.TargetType)
            {
                case "Idea":
                    var idea = await _db.Ideas.FirstOrDefaultAsync(i => i.Id == report.TargetId, ct);
                    if (idea is not null) _db.Ideas.Remove(idea);
                    break;
                case "Post":
                    var post = await _db.CommunityPosts.FirstOrDefaultAsync(p => p.Id == report.TargetId, ct);
                    if (post is not null) _db.CommunityPosts.Remove(post);
                    break;
                case "Comment":
                    var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == report.TargetId, ct);
                    if (comment is not null) _db.Comments.Remove(comment);
                    break;
            }
            report.Status = "ActionTaken";
        }
        else
        {
            // Dismissed: clear the flag so the content stops showing as
            // suspect in the community view.
            if (report.TargetType == "Post")
            {
                var post = await _db.CommunityPosts.FirstOrDefaultAsync(p => p.Id == report.TargetId, ct);
                if (post is not null) post.IsFlagged = false;
            }
            report.Status = "Dismissed";
        }

        report.ResolvedById = adminId;
        report.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
