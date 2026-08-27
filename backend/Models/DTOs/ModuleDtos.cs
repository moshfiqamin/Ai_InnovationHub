// ============================================================
// MODULES: M7 · M8 · M9 · M10 · M11 · M12 · M13 · M14
// LAYER  : Model (MVC: M) — Data Transfer Objects
// FEATURES: F5, F6, F12, F13, F14, F15, F16, F17, F19, F20
// ============================================================
using System.ComponentModel.DataAnnotations;

namespace AiInnovationHub.Api.Models.DTOs;

// ================= M7 : COMMUNITY (F5) =================
public class CommunityRequest
{
    [Required(ErrorMessage = "A community name is required.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 120 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)] public string Description { get; set; } = string.Empty;
    [StringLength(80)]   public string Category { get; set; } = string.Empty;
}

public class CommunityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public int PostCount { get; set; }
    public bool JoinedByMe { get; set; }
}

public class PostRequest
{
    [Required(ErrorMessage = "A post title is required.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Post content cannot be empty.")]
    [StringLength(5000, MinimumLength = 5, ErrorMessage = "Content must be at least 5 characters.")]
    public string Content { get; set; } = string.Empty;
}

public class PostDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public int Upvotes { get; set; }
    public int CommentCount { get; set; }
    public bool UpvotedByMe { get; set; }
    public bool IsFlagged { get; set; }
    public string TimeAgo { get; set; } = string.Empty;
    public List<CommentDto> Comments { get; set; } = new();
}

// ================= M8 : SMART SEARCH (F6) =================
public class SearchResultDto
{
    public string Type { get; set; } = string.Empty;   // Idea | Project | User | Community
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public double Score { get; set; }                  // relevance, 0..1
}

public class SearchResponse
{
    public List<SearchResultDto> Results { get; set; } = new();
    // "semantic" when embeddings were used, "keyword" when they were not.
    public string Mode { get; set; } = "keyword";
    public string Query { get; set; } = string.Empty;
}

// ================= M8 : BUSINESS MODEL (F12) =================
public class BusinessModelDto
{
    public string ValueProposition { get; set; } = string.Empty;
    public List<string> CustomerSegments { get; set; } = new();
    public List<string> RevenueStreams { get; set; } = new();
    public List<string> KeyResources { get; set; } = new();
    public List<string> KeyPartners { get; set; } = new();
    public List<string> CostStructure { get; set; } = new();
    public List<string> Channels { get; set; } = new();
}

// ================= M9 : CHALLENGES (F14) =================
public class ChallengeRequest
{
    [Required(ErrorMessage = "A challenge title is required.")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)] public string Description { get; set; } = string.Empty;
    [StringLength(80)]   public string Category { get; set; } = string.Empty;
    [StringLength(200)]  public string Prize { get; set; } = string.Empty;

    [Required(ErrorMessage = "A deadline is required.")]
    public DateTime Deadline { get; set; }
}

public class ChallengeDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Prize { get; set; } = string.Empty;
    public DateTime Deadline { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public int SubmissionCount { get; set; }
    public bool JoinedByMe { get; set; }
    public bool CanManage { get; set; }
    public int DaysLeft { get; set; }
}

public class SubmissionRequest
{
    [Required] public Guid IdeaId { get; set; }
}

public class ScoreRequest
{
    [Range(0, 100, ErrorMessage = "Score must be between 0 and 100.")]
    public int Score { get; set; }
    [StringLength(2000)] public string Feedback { get; set; } = string.Empty;
}

public class SubmissionDto
{
    public Guid Id { get; set; }
    public Guid IdeaId { get; set; }
    public string IdeaTitle { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? Score { get; set; }
    public string? Feedback { get; set; }
    public int Rank { get; set; }
}

// ================= M10 : MENTOR / INVESTOR (F13, F15) =================
public class MentorDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public string Expertise { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public int ReputationPoints { get; set; }
    public bool IsAvailable { get; set; }
    // Populated only by the AI recommendation endpoint (F13)
    public string? WhyRecommended { get; set; }
}

public class InvestorDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public string InvestmentFocus { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
}

public class MentorshipRequestDto
{
    [Required(ErrorMessage = "Please include a short message.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Message must be at least 10 characters.")]
    public string Message { get; set; } = string.Empty;
}

public class InvestmentRequestDto
{
    [Required] public Guid ProjectId { get; set; }
    [StringLength(2000)] public string Message { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
}

public class EngagementDto
{
    public Guid Id { get; set; }
    public string CounterpartName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;   // project title / "Mentorship"
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public string Direction { get; set; } = string.Empty; // Incoming | Outgoing
    public string TimeAgo { get; set; } = string.Empty;
}

public class StatusRequest
{
    [Required] public string Status { get; set; } = "Accepted";  // Accepted | Declined
}

// ================= M11 : ANALYTICS (F19) =================
public class PlatformAnalyticsDto
{
    public int TotalIdeas { get; set; }
    public int TotalProjects { get; set; }
    public int TotalCommunities { get; set; }
    public int TotalChallenges { get; set; }
    public int MyIdeas { get; set; }
    public int MyUpvotesReceived { get; set; }
    public int MyComments { get; set; }
    public int MyReputation { get; set; }

    public ChartSeries IdeasOverTime { get; set; } = new();
    public ChartSeries CategoryBreakdown { get; set; } = new();
    public ChartSeries EngagementByType { get; set; } = new();
    public List<TrendingIdeaDto> TopIdeas { get; set; } = new();
}

// ================= M12 : NOTIFICATIONS (F17) =================
public class NotificationDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Link { get; set; }
    public bool IsRead { get; set; }
    public string TimeAgo { get; set; } = string.Empty;
}

// ================= M13 : PROFILE (F16) =================
public class ProfileUpdateRequest
{
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters.")]
    public string FullName { get; set; } = string.Empty;
    [StringLength(1000)] public string Bio { get; set; } = string.Empty;
    [StringLength(150)]  public string Headline { get; set; } = string.Empty;
    [StringLength(120)]  public string Location { get; set; } = string.Empty;
    [StringLength(200)]  public string Website { get; set; } = string.Empty;
    [StringLength(300)]  public string Skills { get; set; } = string.Empty;
    [StringLength(300)]  public string Interests { get; set; } = string.Empty;
    [StringLength(300)]  public string Expertise { get; set; } = string.Empty;
    [StringLength(300)]  public string InvestmentFocus { get; set; } = string.Empty;
    public bool IsAvailableForMentoring { get; set; }
}

public class BadgeDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool Earned { get; set; }
    public int Progress { get; set; }       // current value of the metric
    public int Threshold { get; set; }
}

public class ProfileDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string Skills { get; set; } = string.Empty;
    public string Interests { get; set; } = string.Empty;
    public string Expertise { get; set; } = string.Empty;
    public string InvestmentFocus { get; set; } = string.Empty;
    public bool IsAvailableForMentoring { get; set; }

    public int ReputationPoints { get; set; }
    public string Level { get; set; } = string.Empty;   // derived from reputation
    public int IdeaCount { get; set; }
    public int ProjectCount { get; set; }
    public int CommentCount { get; set; }
    public bool IsMe { get; set; }

    public List<BadgeDto> Badges { get; set; } = new();
    public List<IdeaCardDto> RecentIdeas { get; set; } = new();
    public List<ActivityDto> RecentActivity { get; set; } = new();
}

// ================= M14 : ADMIN (F20) =================
public class ReportRequest
{
    [Required] public string TargetType { get; set; } = "Idea";
    [Required] public Guid TargetId { get; set; }
    [Required(ErrorMessage = "Please say why you are reporting this.")]
    [StringLength(500, MinimumLength = 5)]
    public string Reason { get; set; } = string.Empty;
}

public class ReportDto
{
    public Guid Id { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public Guid TargetId { get; set; }
    public string TargetPreview { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ReporterName { get; set; }
    public string? AiVerdict { get; set; }
    public string? AiReason { get; set; }
    public string TimeAgo { get; set; } = string.Empty;
}

public class AdminStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalIdeas { get; set; }
    public int TotalProjects { get; set; }
    public int TotalCommunities { get; set; }
    public int TotalChallenges { get; set; }
    public int PendingReports { get; set; }
    public int FlaggedContent { get; set; }
    public ChartSeries UsersByRole { get; set; } = new();
    public ChartSeries SignupsOverTime { get; set; } = new();
}

public class AdminUserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int ReputationPoints { get; set; }
    public int IdeaCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminRoleRequest
{
    [Required] public string Role { get; set; } = "Innovator";
}
