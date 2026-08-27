// ============================================================
// MODULE : M4 — Innovation Feed
// LAYER  : Model (MVC: M)
// FEATURE: F4 — the bookmark/save interaction
// ============================================================
namespace AiInnovationHub.Api.Models.Entities;

public class IdeaBookmark
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid IdeaId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public Idea? Idea { get; set; }
}
