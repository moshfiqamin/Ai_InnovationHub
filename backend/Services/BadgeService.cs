// ============================================================
// MODULE : M13 — Profile
// LAYER  : Service
// FEATURE: F16 — Reputation & Badge System
// PURPOSE: Seeds the badge catalogue, evaluates which badges a user
//          has earned, and awards new ones. Awarding raises an F17
//          notification so the user finds out.
// ============================================================
using AiInnovationHub.Api.Data;
using AiInnovationHub.Api.Models.DTOs;
using AiInnovationHub.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiInnovationHub.Api.Services;

public interface IBadgeService
{
    Task SeedAsync(CancellationToken ct = default);
    Task<List<BadgeDto>> EvaluateAsync(Guid userId, CancellationToken ct = default);
    Task<int> AwardNewAsync(Guid userId, CancellationToken ct = default);
    static string LevelFor(int reputation) =>
        reputation >= 500 ? "Luminary"
        : reputation >= 250 ? "Innovator III"
        : reputation >= 100 ? "Innovator II"
        : reputation >= 25  ? "Innovator I"
        : "Newcomer";
}

public class BadgeService : IBadgeService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notify;

    public BadgeService(AppDbContext db, INotificationService notify)
    {
        _db = db; _notify = notify;
    }

    // ---- THE CATALOGUE ----
    // Seeded on startup. Code is the stable key, so renaming a badge
    // never re-awards it.
    private static readonly Badge[] Catalogue =
    {
        new() { Code="first_idea",    Name="First Spark",     Icon="💡", Metric="ideas",      Threshold=1,   Description="Published your first idea." },
        new() { Code="idea_5",        Name="Idea Machine",    Icon="⚡", Metric="ideas",      Threshold=5,   Description="Published five ideas." },
        new() { Code="idea_10",       Name="Prolific",        Icon="🌟", Metric="ideas",      Threshold=10,  Description="Published ten ideas." },
        new() { Code="first_upvote",  Name="Recognised",      Icon="👍", Metric="upvotes",    Threshold=1,   Description="Received your first upvote." },
        new() { Code="upvote_25",     Name="Crowd Favourite", Icon="🔥", Metric="upvotes",    Threshold=25,  Description="Received 25 upvotes." },
        new() { Code="upvote_100",    Name="Community Star",  Icon="🏆", Metric="upvotes",    Threshold=100, Description="Received 100 upvotes." },
        new() { Code="first_comment", Name="Conversationalist",Icon="💬",Metric="comments",   Threshold=1,   Description="Left your first comment." },
        new() { Code="comment_25",    Name="Discussion Lead", Icon="🗣️", Metric="comments",   Threshold=25,  Description="Left 25 comments." },
        new() { Code="first_project", Name="Builder",         Icon="🚀", Metric="projects",   Threshold=1,   Description="Started your first project." },
        new() { Code="project_3",     Name="Serial Builder",  Icon="🏗️", Metric="projects",   Threshold=3,   Description="Started three projects." },
        new() { Code="rep_100",       Name="Trusted Voice",   Icon="🎖️", Metric="reputation", Threshold=100, Description="Reached 100 reputation." },
        new() { Code="rep_500",       Name="Luminary",        Icon="👑", Metric="reputation", Threshold=500, Description="Reached 500 reputation." },
    };

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var existing = await _db.Badges.Select(b => b.Code).ToListAsync(ct);
        var missing = Catalogue.Where(b => !existing.Contains(b.Code)).ToList();
        if (missing.Count == 0) return;

        // Fresh instances so EF does not track the static array.
        _db.Badges.AddRange(missing.Select(b => new Badge
        {
            Code = b.Code, Name = b.Name, Description = b.Description,
            Icon = b.Icon, Metric = b.Metric, Threshold = b.Threshold,
        }));
        await _db.SaveChangesAsync(ct);
    }

    // ---- CURRENT VALUE OF EVERY METRIC FOR ONE USER ----
    private async Task<Dictionary<string, int>> MetricsAsync(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        return new Dictionary<string, int>
        {
            ["ideas"]    = await _db.Ideas.CountAsync(i => i.AuthorId == userId && i.IsPublished, ct),
            // Upvotes RECEIVED across all of this user's ideas.
            ["upvotes"]  = await _db.Ideas.Where(i => i.AuthorId == userId).SumAsync(i => (int?)i.Upvotes, ct) ?? 0,
            ["comments"] = await _db.Comments.CountAsync(c => c.AuthorId == userId, ct)
                         + await _db.PostComments.CountAsync(c => c.AuthorId == userId, ct),
            ["projects"] = await _db.Projects.CountAsync(p => p.OwnerId == userId, ct),
            ["reputation"] = user?.ReputationPoints ?? 0,
        };
    }

    // ---- WHAT THE PROFILE PAGE SHOWS ----
    // Every badge, earned or not, with progress toward it.
    public async Task<List<BadgeDto>> EvaluateAsync(Guid userId, CancellationToken ct = default)
    {
        var badges = await _db.Badges.AsNoTracking().ToListAsync(ct);
        var earned = await _db.UserBadges.Where(x => x.UserId == userId)
                                         .Select(x => x.BadgeId).ToListAsync(ct);
        var metrics = await MetricsAsync(userId, ct);

        return badges
            .Select(b => new BadgeDto
            {
                Code = b.Code, Name = b.Name, Description = b.Description, Icon = b.Icon,
                Earned = earned.Contains(b.Id),
                Progress = Math.Min(metrics.GetValueOrDefault(b.Metric), b.Threshold),
                Threshold = b.Threshold,
            })
            .OrderByDescending(b => b.Earned).ThenBy(b => b.Threshold)
            .ToList();
    }

    // ---- AWARD ANY NEWLY QUALIFIED BADGES ----
    // Returns how many were granted. Called after actions that could
    // move a metric (publishing, commenting, receiving an upvote).
    public async Task<int> AwardNewAsync(Guid userId, CancellationToken ct = default)
    {
        var badges = await _db.Badges.AsNoTracking().ToListAsync(ct);
        var earned = await _db.UserBadges.Where(x => x.UserId == userId)
                                         .Select(x => x.BadgeId).ToListAsync(ct);
        var metrics = await MetricsAsync(userId, ct);

        var newly = badges
            .Where(b => !earned.Contains(b.Id)
                     && metrics.GetValueOrDefault(b.Metric) >= b.Threshold)
            .ToList();
        if (newly.Count == 0) return 0;

        foreach (var b in newly)
        {
            _db.UserBadges.Add(new UserBadge { UserId = userId, BadgeId = b.Id });
            _notify.Push(userId, "Badge", $"{b.Icon} Badge earned: {b.Name} — {b.Description}", "/profile");
        }

        await _db.SaveChangesAsync(ct);
        return newly.Count;
    }
}
