// ============================================================
// MODULE : M3 — Dashboard
// LAYER  : Service — business logic behind DashboardController
// FEATURES:
//   F19 — Analytics Dashboard  (GetSummaryAsync)
//   F18 — AI Personalized Recommendation (GetRecommendationsAsync)
// WHY A SERVICE: Keeping querying/AI logic out of the controller is
//   what "proper MVC separation" means for the course rubric. The
//   controller only handles HTTP; this class holds the logic.
// ============================================================
using System.Text.Json;
using AiInnovationHub.Api.Data;
using AiInnovationHub.Api.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AiInnovationHub.Api.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(Guid userId, CancellationToken ct = default);
    Task<RecommendationsResponse> GetRecommendationsAsync(Guid userId, CancellationToken ct = default);
}

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;
    private readonly IAiProvider _ai;

    public DashboardService(AppDbContext db, IAiProvider ai)
    {
        _db = db;
        _ai = ai;
    }

    // ==========================================================
    // F19 — ANALYTICS DASHBOARD
    // Gathers every number the dashboard renders in one round trip.
    // ==========================================================
    public async Task<DashboardSummaryDto> GetSummaryAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking()
                                  .FirstOrDefaultAsync(u => u.Id == userId, ct);

        // ---- 1. QUICK STATISTICS ----
        var ideasSubmitted = await _db.Ideas.CountAsync(i => i.AuthorId == userId, ct);

        var stats = new DashboardStats
        {
            IdeasSubmitted      = ideasSubmitted,
            ActiveProjects      = 0, // wired up when M6 Project Collaboration is built
            ReputationPoints    = user?.ReputationPoints ?? 0,
            UnreadNotifications = 0, // wired up when M12 Notifications is built
        };

        // ---- 2. ENGAGEMENT CHART (last 7 days) ----
        // Counts this user's ideas created per day, oldest -> newest.
        var today = DateTime.UtcNow.Date;
        var engagement = new ChartSeries();

        for (int offset = 6; offset >= 0; offset--)
        {
            var day = today.AddDays(-offset);
            var nextDay = day.AddDays(1);

            var count = await _db.Ideas.CountAsync(
                i => i.AuthorId == userId && i.CreatedAt >= day && i.CreatedAt < nextDay, ct);

            engagement.Labels.Add(day.ToString("ddd"));  // Mon, Tue, ...
            engagement.Values.Add(count);
        }

        // ---- 3. CONTRIBUTION MIX ----
        // Grouped counts of the user's logged activity by type.
        var activityGroups = await _db.ActivityLogs
            .Where(a => a.UserId == userId)
            .GroupBy(a => a.ActivityType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var mix = new ChartSeries();
        if (activityGroups.Count > 0)
        {
            foreach (var g in activityGroups)
            {
                mix.Labels.Add(g.Type);
                mix.Values.Add(g.Count);
            }
        }
        else
        {
            // Empty-state placeholder so the doughnut still renders.
            mix.Labels.Add("No activity yet");
            mix.Values.Add(1);
        }

        // ---- 4. TRENDING IDEAS (platform-wide, most upvoted) ----
        var trending = await _db.Ideas.AsNoTracking()
            .Where(i => i.IsPublished)
            .OrderByDescending(i => i.Upvotes)
            .ThenByDescending(i => i.CreatedAt)
            .Take(5)
            .Select(i => new TrendingIdeaDto
            {
                Id = i.Id, Title = i.Title, Category = i.Category, Upvotes = i.Upvotes,
            })
            .ToListAsync(ct);

        // ---- 5. RECENT ACTIVITY (this user, newest first) ----
        var activityRows = await _db.ActivityLogs.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(6)
            .ToListAsync(ct);

        var recent = activityRows
            .Select(a => new ActivityDto
            {
                Description = a.Description,
                TimeAgo = FormatTimeAgo(a.CreatedAt),
            })
            .ToList();

        return new DashboardSummaryDto
        {
            Stats = stats,
            Engagement = engagement,
            ContributionMix = mix,
            TrendingIdeas = trending,
            RecentActivity = recent,
        };
    }

    // ==========================================================
    // F18 — AI PERSONALIZED RECOMMENDATION
    // Builds a prompt from the user's profile, asks Gemini for JSON,
    // and falls back to static suggestions if the AI is unavailable.
    // ==========================================================
    public async Task<RecommendationsResponse> GetRecommendationsAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking()
                                  .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return FallbackRecommendations();

        var ideaTitles = await _db.Ideas.AsNoTracking()
            .Where(i => i.AuthorId == userId)
            .OrderByDescending(i => i.CreatedAt)
            .Take(5)
            .Select(i => i.Title)
            .ToListAsync(ct);

        // ---- BUILD THE PROMPT ----
        // Asking for strict JSON makes the reply parseable rather than prose.
        var skills    = string.IsNullOrWhiteSpace(user.Skills)    ? "not specified" : user.Skills;
        var interests = string.IsNullOrWhiteSpace(user.Interests) ? "not specified" : user.Interests;
        var titles    = ideaTitles.Count > 0 ? string.Join("; ", ideaTitles) : "none yet";

        // NOTE: $$ raw string literal. With two '$' the interpolation syntax
        // becomes {{expr}}, which leaves the single braces of the JSON example
        // below as literal characters exactly as the model should return them.
        var prompt = $$"""
        You are an assistant inside AI_InnovationHub, a collaborative innovation platform.
        Recommend exactly 3 next actions for this member.

        Member role       : {{user.Role}}
        Skills            : {{skills}}
        Interests         : {{interests}}
        Ideas submitted   : {{ideaTitles.Count}}
        Recent idea titles: {{titles}}

        Reply with ONLY a JSON array, no markdown fences, in exactly this shape:
        [{"title":"short action","reason":"one sentence explaining why it suits this member"}]
        """;

        var raw = await _ai.GenerateTextAsync(prompt, ct);
        if (string.IsNullOrWhiteSpace(raw)) return FallbackRecommendations();

        // ---- PARSE THE REPLY ----
        try
        {
            // AiJson strips any markdown fence and tolerates a bad reply.
            var items = AiJson.Array<List<RecommendationDto>>(raw);
            if (items is null || items.Count == 0) return FallbackRecommendations();

            // Report the provider that actually answered, not an assumption.
            return new RecommendationsResponse { Items = items, Source = _ai.LastProviderUsed };
        }
        catch
        {
            // Malformed JSON from the model must not break the dashboard.
            return FallbackRecommendations();
        }
    }

    // ---- OFFLINE FALLBACK (NFR10 Reliability) ----
    // Used when the API key is missing, the quota is exhausted, or the
    // reply cannot be parsed. The dashboard still renders something useful.
    private static RecommendationsResponse FallbackRecommendations() => new()
    {
        Source = "fallback",
        Items = new List<RecommendationDto>
        {
            new() { Title = "Submit your first innovation idea",
                    Reason = "Publishing an idea unlocks AI analysis, SWOT and similar-idea matching." },
            new() { Title = "Complete your skills and interests",
                    Reason = "A fuller profile lets the platform match you with relevant mentors and teams." },
            new() { Title = "Join an active community",
                    Reason = "Discussion is the fastest route to collaborators who share your problem space." },
        },
    };

    // ---- HELPER: turn a timestamp into "3 hours ago" ----
    private static string FormatTimeAgo(DateTime utc)
    {
        var span = DateTime.UtcNow - utc;
        if (span.TotalMinutes < 1)  return "just now";
        if (span.TotalMinutes < 60) return Plural((int)span.TotalMinutes, "minute");
        if (span.TotalHours   < 24) return Plural((int)span.TotalHours, "hour");
        if (span.TotalDays    < 30) return Plural((int)span.TotalDays, "day");
        return utc.ToString("d MMM yyyy");
    }

    // Renders "1 day ago" rather than "1 days ago".
    private static string Plural(int count, string unit)
    {
        return count == 1 ? $"1 {unit} ago" : $"{count} {unit}s ago";
    }
}
