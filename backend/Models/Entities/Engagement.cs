// ============================================================
// MODULE : M10 — Mentor & Investor
// LAYER  : Model (MVC: M)
// FEATURES: F13 AI Mentor Recommendation · F15 Investor Connect
// ============================================================
namespace AiInnovationHub.Api.Models.Entities;

public class MentorshipRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Message { get; set; } = string.Empty;
    // Pending | Accepted | Declined
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid MentorId { get; set; }
    public User? Mentor { get; set; }
    public Guid RequesterId { get; set; }
    public User? Requester { get; set; }
}

public class InvestmentInterest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Message { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    // Pending | Accepted | Declined
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid InvestorId { get; set; }
    public User? Investor { get; set; }
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
}
