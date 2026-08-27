// ============================================================
// MODULE : M4 — Innovation Feed (comment entry points)
// LAYER  : Model (MVC: M)
// FEATURE: F4. Also the foundation M7 Community will reuse for F5.
// PURPOSE: A comment on an idea. ParentId being non-null makes the
//          row a reply, which gives one level of threading.
// ============================================================
namespace AiInnovationHub.Api.Models.Entities;

public class Comment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid IdeaId { get; set; }
    public Idea? Idea { get; set; }

    public Guid AuthorId { get; set; }
    public User? Author { get; set; }

    // Null = top-level comment. Set = a reply to that comment.
    public Guid? ParentId { get; set; }
}
