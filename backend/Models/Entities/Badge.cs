// ============================================================
// MODULE : M13 — Profile
// LAYER  : Model (MVC: M)
// FEATURE: F16 — Reputation & Badge System
// PURPOSE: Badge is the catalogue (seeded once); UserBadge records who
//          has earned what. Threshold is the reputation or count needed.
// ============================================================
namespace AiInnovationHub.Api.Models.Entities;

public class Badge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;   // stable key, e.g. "first_idea"
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "🏅";

    // What the user must reach. Metric names the counter to compare.
    // ideas | upvotes | comments | projects | reputation
    public string Metric { get; set; } = "reputation";
    public int Threshold { get; set; } = 1;
}

public class UserBadge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid BadgeId { get; set; }
    public Badge? Badge { get; set; }
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
}
