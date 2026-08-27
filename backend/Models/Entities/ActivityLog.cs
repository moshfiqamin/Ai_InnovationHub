// ============================================================
// MODULE : M3 — Dashboard (Recent activity panel)
// LAYER  : Model (MVC: M) — database entity
// PURPOSE: Append-only record of notable user actions. Also serves
//          NFR15 Auditability and will feed M12 notifications.
// ============================================================
namespace AiInnovationHub.Api.Models.Entities;

public class ActivityLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Human-readable line shown in the dashboard's activity feed
    public string Description { get; set; } = string.Empty;

    // Machine-readable category: Auth | Idea | Project | Challenge ...
    public string ActivityType { get; set; } = "General";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ---- FOREIGN KEY -> User ----
    public Guid UserId { get; set; }
    public User? User { get; set; }
}
