// ============================================================
// MODULE : M4 Innovation Feed + M5 Idea Management
// LAYER  : Model (MVC: M) — Data Transfer Objects
// FEATURES: F1, F2, F3, F4, F11
// ============================================================
using System.ComponentModel.DataAnnotations;

namespace AiInnovationHub.Api.Models.DTOs;

// ---- F1: create / update an idea ----
public class IdeaRequest
{
    [Required(ErrorMessage = "A title is required.")]
    [StringLength(200, MinimumLength = 5,
        ErrorMessage = "Title must be between 5 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Describe the problem your idea solves.")]
    [StringLength(4000, MinimumLength = 10,
        ErrorMessage = "The problem description must be at least 10 characters.")]
    public string Problem { get; set; } = string.Empty;

    [Required(ErrorMessage = "Describe your proposed solution.")]
    [StringLength(4000, MinimumLength = 10,
        ErrorMessage = "The solution description must be at least 10 characters.")]
    public string Solution { get; set; } = string.Empty;

    [StringLength(80)]  public string Category { get; set; } = string.Empty;
    [StringLength(300)] public string Tags { get; set; } = string.Empty;

    // false = save as draft, true = publish straight to the feed
    public bool Publish { get; set; } = false;
}

// ---- F4: one card in the feed ----
public class IdeaCardDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public int Upvotes { get; set; }
    public int CommentCount { get; set; }
    public int Views { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorRole { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string TimeAgo { get; set; } = string.Empty;

    // Per-viewer state so the UI can render filled/outlined icons
    public bool LikedByMe { get; set; }
    public bool BookmarkedByMe { get; set; }
}

// ---- F11: structured SWOT ----
public class SwotDto
{
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public List<string> Opportunities { get; set; } = new();
    public List<string> Threats { get; set; } = new();
}

// ---- F3: one semantically similar idea ----
public class SimilarIdeaDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    // 0..1 cosine similarity, surfaced so the user can judge relevance
    public double Similarity { get; set; }
}

// ---- M5: full idea detail page ----
public class IdeaDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Problem { get; set; } = string.Empty;
    public string Solution { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public bool IsPublished { get; set; }
    public int Upvotes { get; set; }
    public int Views { get; set; }
    public int CommentCount { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public Guid AuthorId { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool LikedByMe { get; set; }
    public bool BookmarkedByMe { get; set; }
    public bool IsMine { get; set; }          // controls edit/publish buttons

    public string? AiAnalysis { get; set; }   // F2
    public SwotDto? Swot { get; set; }        // F11
    public List<CommentDto> Comments { get; set; } = new();
}

// ---- F4: a comment ----
public class CommentDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public string TimeAgo { get; set; } = string.Empty;
}

public class CommentRequest
{
    [Required(ErrorMessage = "Comment cannot be empty.")]
    [StringLength(2000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
}

// ---- Generic toggle result for like/bookmark ----
public class ToggleResult
{
    public bool Active { get; set; }   // is it now on?
    public int Count { get; set; }     // new total
}
