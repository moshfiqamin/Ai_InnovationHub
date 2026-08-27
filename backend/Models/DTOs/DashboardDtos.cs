// ============================================================
// MODULE : M3 — Dashboard
// LAYER  : Model (MVC: M) — Data Transfer Objects
// FEATURES: F18 (recommendations) and F19 (analytics) response shapes
// ============================================================
namespace AiInnovationHub.Api.Models.DTOs;

// ---- F19: the four "quick statistics" tiles ----
public class DashboardStats
{
    public int IdeasSubmitted { get; set; }
    public int ActiveProjects { get; set; }
    public int ReputationPoints { get; set; }
    public int UnreadNotifications { get; set; }
}

// ---- F19: generic chart payload (labels + values) ----
// Used by both the engagement line chart and the contribution doughnut.
public class ChartSeries
{
    public List<string> Labels { get; set; } = new();
    public List<int> Values { get; set; } = new();
}

// ---- M3: one row in the trending ideas panel ----
public class TrendingIdeaDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Upvotes { get; set; }
}

// ---- M3: one row in the recent activity panel ----
public class ActivityDto
{
    public string Description { get; set; } = string.Empty;
    public string TimeAgo { get; set; } = string.Empty; // e.g. "3 hours ago"
}

// ---- M3: the whole /dashboard/summary response ----
public class DashboardSummaryDto
{
    public DashboardStats Stats { get; set; } = new();
    public ChartSeries Engagement { get; set; } = new();
    public ChartSeries ContributionMix { get; set; } = new();
    public List<TrendingIdeaDto> TrendingIdeas { get; set; } = new();
    public List<ActivityDto> RecentActivity { get; set; } = new();
}

// ---- F18: a single AI-generated recommendation ----
public class RecommendationDto
{
    public string Title { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

// ---- F18: the whole /dashboard/recommendations response ----
// 'Source' tells the UI whether Gemini answered or we fell back.
public class RecommendationsResponse
{
    public List<RecommendationDto> Items { get; set; } = new();
    public string Source { get; set; } = "gemini";
}
