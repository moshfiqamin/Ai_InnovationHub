// ============================================================
// MODULE : M9 — Innovation Challenges
// LAYER  : Service
// FEATURE: F14 — Innovation Challenges
// IMPLEMENTS: challenge list/detail, join, submission form, submission
//   status, judging/score view, leaderboard, deadlines.
// AUTHORISATION: only Organization and Admin roles may create or judge
//   challenges, matching requirements.pdf M9.
// ============================================================
using AiInnovationHub.Api.Data;
using AiInnovationHub.Api.Models.DTOs;
using AiInnovationHub.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiInnovationHub.Api.Services;

public interface IChallengeService
{
    Task<List<ChallengeDto>> ListAsync(Guid userId, string? status, CancellationToken ct = default);
    Task<ChallengeDto?> GetAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<(Guid? id, string error)> CreateAsync(Guid userId, ChallengeRequest req, CancellationToken ct = default);
    Task<(bool ok, string error)> SubmitAsync(Guid challengeId, Guid userId, Guid ideaId, CancellationToken ct = default);
    Task<List<SubmissionDto>> SubmissionsAsync(Guid challengeId, CancellationToken ct = default);
    Task<(bool ok, string error)> ScoreAsync(Guid submissionId, Guid userId, ScoreRequest req, CancellationToken ct = default);
}

public class ChallengeService : IChallengeService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notify;

    // Roles permitted to CREATE and own challenges (M9).
    private static readonly string[] OrganiserRoles = { "Organization", "Admin" };

    // Roles permitted to SCORE submissions. A Judge is granted this by an
    // administrator so an organisation can bring in an external panel
    // without handing over ownership of the challenge itself.
    private static readonly string[] JudgingRoles = { "Judge", "Admin" };

    public ChallengeService(AppDbContext db, INotificationService notify)
    {
        _db = db; _notify = notify;
    }

    public async Task<List<ChallengeDto>> ListAsync(Guid userId, string? status, CancellationToken ct = default)
    {
        var q = _db.Challenges.Include(c => c.CreatedBy).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && status != "All")
            q = q.Where(c => c.Status == status);

        var rows = await q.OrderBy(c => c.Deadline).ToListAsync(ct);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

        var result = new List<ChallengeDto>();
        foreach (var c in rows) result.Add(await ToDto(c, userId, user, ct));
        return result;
    }

    public async Task<ChallengeDto?> GetAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var c = await _db.Challenges.Include(x => x.CreatedBy).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return null;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        return await ToDto(c, userId, user, ct);
    }

    // ---- Create (organisers only) ----
    public async Task<(Guid?, string)> CreateAsync(Guid userId, ChallengeRequest req, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null || !OrganiserRoles.Contains(user.Role))
            return (null, "Only Organization or Admin accounts can create challenges.");

        if (req.Deadline <= DateTime.UtcNow)
            return (null, "The deadline must be in the future.");

        var challenge = new Challenge
        {
            Title = req.Title.Trim(), Description = req.Description.Trim(),
            Category = req.Category.Trim(), Prize = req.Prize.Trim(),
            Deadline = req.Deadline.ToUniversalTime(), CreatedById = userId,
        };
        _db.Challenges.Add(challenge);
        await _db.SaveChangesAsync(ct);
        return (challenge.Id, "");
    }

    // ---- Enter one of your own ideas ----
    public async Task<(bool, string)> SubmitAsync(Guid challengeId, Guid userId, Guid ideaId,
                                                  CancellationToken ct = default)
    {
        var challenge = await _db.Challenges.FirstOrDefaultAsync(c => c.Id == challengeId, ct);
        if (challenge is null) return (false, "That challenge does not exist.");

        if (challenge.Status != "Open") return (false, "This challenge is no longer accepting entries.");
        if (challenge.Deadline < DateTime.UtcNow) return (false, "The deadline for this challenge has passed.");

        // You may only submit an idea you authored, and it must be public.
        var idea = await _db.Ideas.FirstOrDefaultAsync(i => i.Id == ideaId, ct);
        if (idea is null || idea.AuthorId != userId) return (false, "You can only submit your own ideas.");
        if (!idea.IsPublished) return (false, "Publish the idea before entering it into a challenge.");

        if (await _db.ChallengeSubmissions.AnyAsync(s => s.ChallengeId == challengeId && s.IdeaId == ideaId, ct))
            return (false, "That idea has already been entered into this challenge.");

        _db.ChallengeSubmissions.Add(new ChallengeSubmission
        {
            ChallengeId = challengeId, IdeaId = ideaId, UserId = userId,
        });
        _db.ActivityLogs.Add(new ActivityLog
        {
            UserId = userId, ActivityType = "Challenge",
            Description = $"Entered \"{Format.Truncate(challenge.Title, 45)}\"",
        });
        _notify.Push(challenge.CreatedById, "Challenge",
            $"New submission for \"{Format.Truncate(challenge.Title, 40)}\".",
            $"/challenges/{challengeId}");

        await _db.SaveChangesAsync(ct);
        return (true, "");
    }

    // ---- Leaderboard: scored entries first, highest score at the top ----
    public async Task<List<SubmissionDto>> SubmissionsAsync(Guid challengeId, CancellationToken ct = default)
    {
        var rows = await _db.ChallengeSubmissions
            .Include(s => s.Idea).Include(s => s.User)
            .Where(s => s.ChallengeId == challengeId)
            .ToListAsync(ct);

        var ordered = rows
            .OrderByDescending(s => s.Score ?? -1)
            .ThenBy(s => s.SubmittedAt)
            .ToList();

        return ordered.Select((s, index) => new SubmissionDto
        {
            Id = s.Id, IdeaId = s.IdeaId,
            IdeaTitle = s.Idea?.Title ?? "Removed idea",
            UserName = s.User?.FullName ?? "Unknown",
            Status = s.Status, Score = s.Score, Feedback = s.Feedback,
            Rank = index + 1,
        }).ToList();
    }

    // ---- Judging (organiser of that challenge only) ----
    public async Task<(bool, string)> ScoreAsync(Guid submissionId, Guid userId, ScoreRequest req,
                                                 CancellationToken ct = default)
    {
        var submission = await _db.ChallengeSubmissions
            .Include(s => s.Challenge).Include(s => s.Idea)
            .FirstOrDefaultAsync(s => s.Id == submissionId, ct);
        if (submission is null) return (false, "That submission does not exist.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        var isOwner = submission.Challenge?.CreatedById == userId;
        // Three ways to earn the right to score: you created the challenge,
        // you hold the Judge role, or you are an administrator.
        if (user is null || (!isOwner && !JudgingRoles.Contains(user.Role)))
            return (false, "Only the challenge organiser, an assigned Judge, or an administrator can score submissions.");

        submission.Score = req.Score;
        submission.Feedback = req.Feedback.Trim();
        submission.Status = "Scored";

        _notify.Push(submission.UserId, "Challenge",
            $"Your entry \"{Format.Truncate(submission.Idea?.Title ?? "", 35)}\" scored {req.Score}/100.",
            $"/challenges/{submission.ChallengeId}");

        await _db.SaveChangesAsync(ct);
        return (true, "");
    }

    // ---- Entity -> DTO, with derived status and days remaining ----
    private async Task<ChallengeDto> ToDto(Challenge c, Guid userId, User? user, CancellationToken ct)
    {
        var daysLeft = (int)Math.Ceiling((c.Deadline - DateTime.UtcNow).TotalDays);
        return new ChallengeDto
        {
            Id = c.Id, Title = c.Title, Description = c.Description,
            Category = c.Category, Prize = c.Prize, Deadline = c.Deadline,
            // Show "Closed" once the deadline passes, even if nobody
            // has explicitly closed it.
            Status = c.Deadline < DateTime.UtcNow ? "Closed" : c.Status,
            CreatedByName = c.CreatedBy?.FullName ?? "Unknown",
            SubmissionCount = await _db.ChallengeSubmissions.CountAsync(s => s.ChallengeId == c.Id, ct),
            JoinedByMe = await _db.ChallengeSubmissions.AnyAsync(s => s.ChallengeId == c.Id && s.UserId == userId, ct),
            CanManage = c.CreatedById == userId || JudgingRoles.Contains(user?.Role ?? ""),
            DaysLeft = Math.Max(0, daysLeft),
        };
    }
}
