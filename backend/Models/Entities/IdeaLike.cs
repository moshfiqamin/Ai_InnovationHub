// ============================================================
// MODULE : M4 — Innovation Feed
// LAYER  : Model (MVC: M)
// FEATURE: F4 — the like/upvote interaction
// PURPOSE: Join row recording that one user upvoted one idea. A
//          composite unique index (UserId, IdeaId) makes double
//          voting impossible at the database level (NFR13).
// ============================================================
namespace AiInnovationHub.Api.Models.Entities;

public class IdeaLike
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid IdeaId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public Idea? Idea { get; set; }
}
