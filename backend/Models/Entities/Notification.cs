// ============================================================
// MODULE : M12 — Notifications
// LAYER  : Model (MVC: M)
// FEATURE: F17 — Notification System
// PURPOSE: One row per alert. Link is a client-side route so the UI can
//          send the user straight to whatever triggered it.
// ============================================================
namespace AiInnovationHub.Api.Models.Entities;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Like | Comment | TeamInvite | TaskAssigned | Mentorship |
    // Investment | Challenge | Badge | Moderation
    public string Type { get; set; } = "General";

    public string Message { get; set; } = string.Empty;
    public string? Link { get; set; }          // e.g. "/ideas/{id}"
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid UserId { get; set; }           // the recipient
    public User? User { get; set; }
}
