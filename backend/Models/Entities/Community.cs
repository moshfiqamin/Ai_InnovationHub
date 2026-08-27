// ============================================================
// MODULE : M7 — Community
// LAYER  : Model (MVC: M)
// FEATURE: F5 — Community Discussion & Comments
// ============================================================
namespace AiInnovationHub.Api.Models.Entities;

public class Community
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public ICollection<CommunityMember> Members { get; set; } = new List<CommunityMember>();
    public ICollection<CommunityPost> Posts { get; set; } = new List<CommunityPost>();
}

public class CommunityMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CommunityId { get; set; }
    public Community? Community { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

public class CommunityPost
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Upvotes { get; set; } = 0;
    public int CommentCount { get; set; } = 0;
    // Set by F20 AI moderation when the text looks problematic.
    public bool IsFlagged { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid CommunityId { get; set; }
    public Community? Community { get; set; }
    public Guid AuthorId { get; set; }
    public User? Author { get; set; }

    public ICollection<PostComment> Comments { get; set; } = new List<PostComment>();
}

public class PostComment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid PostId { get; set; }
    public CommunityPost? Post { get; set; }
    public Guid AuthorId { get; set; }
    public User? Author { get; set; }

    // Null = top-level; set = a reply (one level of threading, as in M4)
    public Guid? ParentId { get; set; }
}

public class PostUpvote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
