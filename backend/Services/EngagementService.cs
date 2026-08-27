// ============================================================
// MODULE : M10 — Mentor & Investor
// LAYER  : Service
// FEATURES: F13 AI Mentor Recommendation · F15 Investor Connect
// ============================================================
using System.Text.Json;
using AiInnovationHub.Api.Data;
using AiInnovationHub.Api.Models.DTOs;
using AiInnovationHub.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiInnovationHub.Api.Services;

public interface IEngagementService
{
    Task<List<MentorDto>> ListMentorsAsync(string? search, CancellationToken ct = default);
    Task<List<MentorDto>> RecommendMentorsAsync(Guid userId, CancellationToken ct = default);
    Task<(bool ok, string error)> RequestMentorshipAsync(Guid mentorId, Guid requesterId, string message, CancellationToken ct = default);
    Task<List<InvestorDto>> ListInvestorsAsync(string? search, CancellationToken ct = default);
    Task<(bool ok, string error)> ExpressInterestAsync(Guid investorId, InvestmentRequestDto req, CancellationToken ct = default);
    Task<List<EngagementDto>> MyEngagementsAsync(Guid userId, CancellationToken ct = default);
    Task<(bool ok, string error)> RespondAsync(Guid id, Guid userId, string kind, string status, CancellationToken ct = default);
}

public class EngagementService : IEngagementService
{
    private readonly AppDbContext _db;
    private readonly IAiProvider _ai;
    private readonly INotificationService _notify;

    public EngagementService(AppDbContext db, IAiProvider ai, INotificationService notify)
    {
        _db = db; _ai = ai; _notify = notify;
    }

    // ---- The mentor directory ----
    public async Task<List<MentorDto>> ListMentorsAsync(string? search, CancellationToken ct = default)
    {
        var q = _db.Users.Where(u => u.Role == "Mentor");
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            q = q.Where(u => EF.Functions.ILike(u.FullName, term)
                          || EF.Functions.ILike(u.Expertise, term)
                          || EF.Functions.ILike(u.Headline, term));
        }

        var rows = await q.OrderByDescending(u => u.ReputationPoints).Take(50).ToListAsync(ct);
        return rows.Select(ToMentorDto).ToList();
    }

    // ==========================================================
    // F13 — AI MENTOR RECOMMENDATION
    // Builds a shortlist from the directory, asks the model which
    // three fit this member's skills and interests, and explains why.
    // Falls back to a reputation ranking if the AI is unavailable.
    // ==========================================================
    public async Task<List<MentorDto>> RecommendMentorsAsync(Guid userId, CancellationToken ct = default)
    {
        var me = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        var mentors = await _db.Users.Where(u => u.Role == "Mentor" && u.Id != userId)
                                     .Take(25).ToListAsync(ct);
        if (me is null || mentors.Count == 0) return new();

        var myIdeas = await _db.Ideas.Where(i => i.AuthorId == userId)
            .OrderByDescending(i => i.CreatedAt).Take(3)
            .Select(i => i.Title).ToListAsync(ct);

        // Number the shortlist so the model can answer with indices —
        // far more reliable than asking it to echo names or GUIDs.
        var roster = string.Join("\n", mentors.Select((m, i) =>
            $"{i}. {m.FullName} — expertise: {(string.IsNullOrWhiteSpace(m.Expertise) ? "unspecified" : m.Expertise)}; {m.Headline}"));

        var prompt = $$"""
        Recommend the 3 most suitable mentors for this member.

        Member skills   : {{(string.IsNullOrWhiteSpace(me.Skills) ? "unspecified" : me.Skills)}}
        Member interests: {{(string.IsNullOrWhiteSpace(me.Interests) ? "unspecified" : me.Interests)}}
        Recent ideas    : {{(myIdeas.Count > 0 ? string.Join("; ", myIdeas) : "none yet")}}

        Available mentors:
        {{roster}}

        Reply with ONLY a JSON array, no markdown fences:
        [{"index":0,"why":"one sentence on why this mentor fits"}]
        """;

        var raw = await _ai.GenerateTextAsync(prompt, ct);

        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                var json = AiJson.Slice(raw, '[', ']');
                if (json is not null)
                {
                    using var doc = JsonDocument.Parse(json);
                    var picks = new List<MentorDto>();

                    foreach (var el in doc.RootElement.EnumerateArray())
                    {
                        var idx = el.GetProperty("index").GetInt32();
                        // Guard against the model inventing an index.
                        if (idx < 0 || idx >= mentors.Count) continue;

                        var dto = ToMentorDto(mentors[idx]);
                        dto.WhyRecommended = el.TryGetProperty("why", out var w) ? w.GetString() : null;
                        if (picks.All(p => p.UserId != dto.UserId)) picks.Add(dto);
                    }
                    if (picks.Count > 0) return picks.Take(3).ToList();
                }
            }
            catch { /* fall through to the ranking below */ }
        }

        // ---- FALLBACK (NFR10): highest-reputation available mentors ----
        return mentors
            .OrderByDescending(m => m.IsAvailableForMentoring).ThenByDescending(m => m.ReputationPoints)
            .Take(3)
            .Select(m => { var d = ToMentorDto(m); d.WhyRecommended = "Ranked by reputation (AI unavailable)."; return d; })
            .ToList();
    }

    // ---- F13: send a mentorship request ----
    public async Task<(bool, string)> RequestMentorshipAsync(Guid mentorId, Guid requesterId, string message,
                                                             CancellationToken ct = default)
    {
        if (mentorId == requesterId) return (false, "You cannot request mentorship from yourself.");

        var mentor = await _db.Users.FirstOrDefaultAsync(u => u.Id == mentorId && u.Role == "Mentor", ct);
        if (mentor is null) return (false, "That mentor does not exist.");

        var pending = await _db.MentorshipRequests.AnyAsync(
            r => r.MentorId == mentorId && r.RequesterId == requesterId && r.Status == "Pending", ct);
        if (pending) return (false, "You already have a pending request with this mentor.");

        _db.MentorshipRequests.Add(new MentorshipRequest
        {
            MentorId = mentorId, RequesterId = requesterId, Message = message.Trim(),
        });
        _notify.Push(mentorId, "Mentorship", "You have a new mentorship request.", "/mentors");
        await _db.SaveChangesAsync(ct);
        return (true, "");
    }

    // ---- F15: the investor directory ----
    public async Task<List<InvestorDto>> ListInvestorsAsync(string? search, CancellationToken ct = default)
    {
        var q = _db.Users.Where(u => u.Role == "Investor");
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            q = q.Where(u => EF.Functions.ILike(u.FullName, term)
                          || EF.Functions.ILike(u.InvestmentFocus, term));
        }

        var rows = await q.Take(50).ToListAsync(ct);
        return rows.Select(u => new InvestorDto
        {
            UserId = u.Id, FullName = u.FullName, Headline = u.Headline,
            InvestmentFocus = u.InvestmentFocus, Bio = u.Bio,
        }).ToList();
    }

    // ==========================================================
    // F15 — INVESTOR CONNECT
    // A project owner registers funding interest with an investor.
    // ==========================================================
    public async Task<(bool, string)> ExpressInterestAsync(Guid requesterId, InvestmentRequestDto req,
                                                           CancellationToken ct = default)
    {
        // Only the project's owner may pitch it.
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == req.ProjectId, ct);
        if (project is null) return (false, "That project does not exist.");
        if (project.OwnerId != requesterId) return (false, "Only the project owner can approach investors.");

        var investors = await _db.Users.Where(u => u.Role == "Investor").Select(u => u.Id).ToListAsync(ct);
        if (investors.Count == 0) return (false, "There are no investors on the platform yet.");

        // Register interest with every investor whose focus matches — in
        // this build we notify all of them, which is the simple, honest
        // behaviour for a course-scale directory.
        foreach (var investorId in investors)
        {
            var duplicate = await _db.InvestmentInterests.AnyAsync(
                i => i.InvestorId == investorId && i.ProjectId == req.ProjectId && i.Status == "Pending", ct);
            if (duplicate) continue;

            _db.InvestmentInterests.Add(new InvestmentInterest
            {
                InvestorId = investorId, ProjectId = req.ProjectId,
                Message = req.Message.Trim(), Amount = req.Amount,
            });
            _notify.Push(investorId, "Investment",
                $"Funding interest registered for \"{Format.Truncate(project.Title, 40)}\".",
                "/investors");
        }

        await _db.SaveChangesAsync(ct);
        return (true, "");
    }

    // ---- Both request types, incoming and outgoing, in one list ----
    public async Task<List<EngagementDto>> MyEngagementsAsync(Guid userId, CancellationToken ct = default)
    {
        var list = new List<EngagementDto>();

        var mentorships = await _db.MentorshipRequests
            .Include(r => r.Mentor).Include(r => r.Requester)
            .Where(r => r.MentorId == userId || r.RequesterId == userId)
            .ToListAsync(ct);

        list.AddRange(mentorships.Select(r => new EngagementDto
        {
            Id = r.Id,
            CounterpartName = r.MentorId == userId
                ? (r.Requester?.FullName ?? "Unknown")
                : (r.Mentor?.FullName ?? "Unknown"),
            Subject = "Mentorship", Message = r.Message, Status = r.Status,
            Direction = r.MentorId == userId ? "Incoming" : "Outgoing",
            TimeAgo = Format.TimeAgo(r.CreatedAt),
        }));

        var investments = await _db.InvestmentInterests
            .Include(i => i.Investor).Include(i => i.Project)
            .Where(i => i.InvestorId == userId || (i.Project != null && i.Project.OwnerId == userId))
            .ToListAsync(ct);

        list.AddRange(investments.Select(i => new EngagementDto
        {
            Id = i.Id,
            CounterpartName = i.InvestorId == userId
                ? (i.Project?.Title ?? "Project")
                : (i.Investor?.FullName ?? "Investor"),
            Subject = i.Project?.Title ?? "Investment", Message = i.Message,
            Status = i.Status, Amount = i.Amount,
            Direction = i.InvestorId == userId ? "Incoming" : "Outgoing",
            TimeAgo = Format.TimeAgo(i.CreatedAt),
        }));

        return list.OrderBy(e => e.Status != "Pending").ToList();
    }

    // ---- Accept or decline. Only the receiving side may respond. ----
    public async Task<(bool, string)> RespondAsync(Guid id, Guid userId, string kind, string status,
                                                   CancellationToken ct = default)
    {
        if (status is not ("Accepted" or "Declined")) return (false, "Status must be Accepted or Declined.");

        if (kind == "mentorship")
        {
            var r = await _db.MentorshipRequests.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (r is null) return (false, "That request does not exist.");
            if (r.MentorId != userId) return (false, "Only the mentor can respond to this request.");

            r.Status = status;
            _notify.Push(r.RequesterId, "Mentorship", $"Your mentorship request was {status.ToLower()}.", "/mentors");
        }
        else
        {
            var i = await _db.InvestmentInterests.Include(x => x.Project)
                                                 .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (i is null) return (false, "That interest does not exist.");
            if (i.InvestorId != userId) return (false, "Only the investor can respond to this.");

            i.Status = status;
            if (i.Project is not null)
                _notify.Push(i.Project.OwnerId, "Investment",
                    $"An investor {status.ToLower()} interest in \"{Format.Truncate(i.Project.Title, 35)}\".",
                    "/investors");
        }

        await _db.SaveChangesAsync(ct);
        return (true, "");
    }

    private static MentorDto ToMentorDto(User u) => new()
    {
        UserId = u.Id, FullName = u.FullName, Headline = u.Headline,
        Expertise = u.Expertise, Bio = u.Bio,
        ReputationPoints = u.ReputationPoints, IsAvailable = u.IsAvailableForMentoring,
    };
}
