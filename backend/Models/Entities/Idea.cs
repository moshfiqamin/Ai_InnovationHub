// ============================================================
// MODULE : M5 — Idea Management
// LAYER  : Model (MVC: M) — database entity
// FEATURES: F1 Idea Submission · F2 AI Analysis
//           F3 Similar Idea Detection · F11 SWOT Analysis
// ============================================================
namespace AiInnovationHub.Api.Models.Entities;

public class Idea
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // ---- F1: the structured idea fields required by requirements.pdf ----
    public string Title { get; set; } = string.Empty;
    public string Problem { get; set; } = string.Empty;   // what is wrong today
    public string Solution { get; set; } = string.Empty;  // the proposed fix
    public string Category { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;      // comma separated
    public string Summary { get; set; } = string.Empty;   // short teaser for cards

    // ---- F1: draft vs published ----
    // Drafts are visible only to their author; publishing exposes them
    // to the M4 feed.
    public bool IsPublished { get; set; } = false;
    public DateTime? PublishedAt { get; set; }

    // ---- Engagement counters (denormalised for fast feed queries) ----
    public int Upvotes { get; set; } = 0;
    public int Views { get; set; } = 0;
    public int CommentCount { get; set; } = 0;

    // ---- F2: AI analysis result (Markdown text from Gemini) ----
    public string? AiAnalysis { get; set; }
    public DateTime? AiAnalysisAt { get; set; }

    // ---- F11: SWOT result, stored as JSON ----
    // Shape: {"strengths":[],"weaknesses":[],"opportunities":[],"threats":[]}
    public string? SwotJson { get; set; }
    public DateTime? SwotAt { get; set; }

    // ---- F12: business model canvas, stored as JSON ----
    public string? BusinessModelJson { get; set; }

    // ---- F3: embedding vector for semantic similarity ----
    // Stored as a JSON float array rather than a pgvector column because
    // pgvector has no PostgreSQL 16 build (see PROJECT_REFERENCE O7).
    // Cosine similarity is computed in SimilarityHelper instead.
    public string? EmbeddingJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ---- RELATIONSHIPS ----
    public Guid AuthorId { get; set; }
    public User? Author { get; set; }

    public ICollection<IdeaLike> Likes { get; set; } = new List<IdeaLike>();
    public ICollection<IdeaBookmark> Bookmarks { get; set; } = new List<IdeaBookmark>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
