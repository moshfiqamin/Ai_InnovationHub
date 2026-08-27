// ============================================================
// MODULE : M11 — Analytics
// LAYER  : Service
// FEATURE: F19 — Analytics Dashboard (the detailed view; M3 shows
//          the summary version of the same data)
// ============================================================
using AiInnovationHub.Api.Data;
using AiInnovationHub.Api.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AiInnovationHub.Api.Services;

public interface IAnalyticsService
{
    Task<PlatformAnalyticsDto> GetAsync(Guid userId, CancellationToken ct = default);
}

public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _db;
    public AnalyticsService(AppDbContext db) => _db = db;

    public async Task<PlatformAnalyticsDto> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);

        var dto = new PlatformAnalyticsDto
        {
            // ---- Platform-wide totals ----
            TotalIdeas       = await _db.Ideas.CountAsync(i => i.IsPublished, ct),
            TotalProjects    = await _db.Projects.CountAsync(ct),
            TotalCommunities = await _db.Communities.CountAsync(ct),
            TotalChallenges  = await _db.Challenges.CountAsync(ct),

            // ---- This member's own numbers ----
            MyIdeas = await _db.Ideas.CountAsync(i => i.AuthorId == userId && i.IsPublished, ct),
            MyUpvotesReceived = await _db.Ideas.Where(i => i.AuthorId == userId)
                                               .SumAsync(i => (int?)i.Upvotes, ct) ?? 0,
            MyComments = await _db.Comments.CountAsync(c => c.AuthorId == userId, ct)
                       + await _db.PostComments.CountAsync(c => c.AuthorId == userId, ct),
            MyReputation = user?.ReputationPoints ?? 0,
        };

        // ---- Ideas published per day over the last 14 days ----
        var today = DateTime.UtcNow.Date;
        for (int offset = 13; offset >= 0; offset--)
        {
            var day = today.AddDays(-offset);
            var next = day.AddDays(1);
            var count = await _db.Ideas.CountAsync(
                i => i.IsPublished && i.PublishedAt >= day && i.PublishedAt < next, ct);

            // Label every other day so a 14-point axis stays readable.
            dto.IdeasOverTime.Labels.Add(offset % 2 == 0 ? day.ToString("d MMM") : "");
            dto.IdeasOverTime.Values.Add(count);
        }

        // ---- Ideas grouped by category ----
        var byCategory = await _db.Ideas
            .Where(i => i.IsPublished && i.Category != "")
            .GroupBy(i => i.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count).Take(6)
            .ToListAsync(ct);

        foreach (var g in byCategory)
        {
            dto.CategoryBreakdown.Labels.Add(g.Category);
            dto.CategoryBreakdown.Values.Add(g.Count);
        }
        if (byCategory.Count == 0)
        {
            dto.CategoryBreakdown.Labels.Add("No published ideas");
            dto.CategoryBreakdown.Values.Add(1);
        }

        // ---- Where engagement across the platform comes from ----
        dto.EngagementByType.Labels.AddRange(new[] { "Upvotes", "Comments", "Bookmarks", "Posts" });
        dto.EngagementByType.Values.AddRange(new[]
        {
            await _db.IdeaLikes.CountAsync(ct),
            await _db.Comments.CountAsync(ct) + await _db.PostComments.CountAsync(ct),
            await _db.IdeaBookmarks.CountAsync(ct),
            await _db.CommunityPosts.CountAsync(ct),
        });

        // ---- Top ideas by upvotes ----
        dto.TopIdeas = await _db.Ideas.AsNoTracking()
            .Where(i => i.IsPublished)
            .OrderByDescending(i => i.Upvotes).Take(5)
            .Select(i => new TrendingIdeaDto
            {
                Id = i.Id, Title = i.Title, Category = i.Category, Upvotes = i.Upvotes,
            })
            .ToListAsync(ct);

        return dto;
    }
}
