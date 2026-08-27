// ============================================================
// MODULE : M5 — Idea Management
// LAYER  : Service — business logic behind IdeasController
// FEATURES:
//   F1  Idea Submission System   (Create/Update/Publish)
//   F2  AI Idea Analysis         (AnalyzeAsync)
//   F3  AI Similar Idea Detection(FindSimilarAsync)
//   F11 AI SWOT Analysis         (GenerateSwotAsync)
// ============================================================
using System.Text.Json;
using AiInnovationHub.Api.Data;
using AiInnovationHub.Api.Models.DTOs;
using AiInnovationHub.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiInnovationHub.Api.Services;

public interface IIdeaService
{
    Task<IdeaDetailDto?> GetByIdAsync(Guid ideaId, Guid viewerId, CancellationToken ct = default);
    Task<List<IdeaCardDto>> GetMineAsync(Guid userId, CancellationToken ct = default);
    Task<Guid> CreateAsync(Guid authorId, IdeaRequest req, CancellationToken ct = default);
    Task<bool> UpdateAsync(Guid ideaId, Guid userId, IdeaRequest req, CancellationToken ct = default);
    Task<bool> PublishAsync(Guid ideaId, Guid userId, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid ideaId, Guid userId, CancellationToken ct = default);
    Task<string?> AnalyzeAsync(Guid ideaId, Guid userId, CancellationToken ct = default);
    Task<SwotDto?> GenerateSwotAsync(Guid ideaId, Guid userId, CancellationToken ct = default);
    Task<List<SimilarIdeaDto>> FindSimilarAsync(Guid ideaId, CancellationToken ct = default);
    Task<BusinessModelDto?> GenerateBusinessModelAsync(Guid ideaId, Guid userId, CancellationToken ct = default);
}

public class IdeaService : IIdeaService
{
    private readonly AppDbContext _db;
    private readonly IAiProvider _ai;
    private readonly ILogger<IdeaService> _log;

    public IdeaService(AppDbContext db, IAiProvider ai, ILogger<IdeaService> log)
    {
        _db = db; _ai = ai; _log = log;
    }

    // ==========================================================
    // F1 — CREATE
    // Saves the idea, generates its embedding for F3, and logs the
    // activity so it appears on the M3 dashboard.
    // ==========================================================
    public async Task<Guid> CreateAsync(Guid authorId, IdeaRequest req, CancellationToken ct = default)
    {
        var idea = new Idea
        {
            AuthorId    = authorId,
            Title       = req.Title.Trim(),
            Problem     = req.Problem.Trim(),
            Solution    = req.Solution.Trim(),
            Category    = req.Category.Trim(),
            Tags        = req.Tags.Trim(),
            Summary     = Format.Truncate(req.Solution.Trim(), 180),
            IsPublished = req.Publish,
            PublishedAt = req.Publish ? DateTime.UtcNow : null,
        };

        // F3 groundwork: embed the idea now so similarity search works later.
        idea.EmbeddingJson = await BuildEmbeddingAsync(idea, ct);

        _db.Ideas.Add(idea);
        _db.ActivityLogs.Add(new ActivityLog
        {
            UserId = authorId,
            ActivityType = "Idea",
            Description = req.Publish
                ? $"Published \"{Format.Truncate(idea.Title, 60)}\""
                : $"Drafted \"{Format.Truncate(idea.Title, 60)}\"",
        });

        // Publishing a real idea earns reputation (groundwork for F16).
        if (req.Publish)
        {
            var author = await _db.Users.FirstOrDefaultAsync(u => u.Id == authorId, ct);
            if (author is not null) author.ReputationPoints += 10;
        }

        await _db.SaveChangesAsync(ct);
        return idea.Id;
    }

    // ==========================================================
    // F1 — UPDATE (author only)
    // ==========================================================
    public async Task<bool> UpdateAsync(Guid ideaId, Guid userId, IdeaRequest req, CancellationToken ct = default)
    {
        var idea = await _db.Ideas.FirstOrDefaultAsync(i => i.Id == ideaId, ct);
        // Ownership check: someone else's idea is not editable.
        if (idea is null || idea.AuthorId != userId) return false;

        idea.Title     = req.Title.Trim();
        idea.Problem   = req.Problem.Trim();
        idea.Solution  = req.Solution.Trim();
        idea.Category  = req.Category.Trim();
        idea.Tags      = req.Tags.Trim();
        idea.Summary   = Format.Truncate(req.Solution.Trim(), 180);
        idea.UpdatedAt = DateTime.UtcNow;

        // The text changed, so the old embedding is stale — regenerate.
        idea.EmbeddingJson = await BuildEmbeddingAsync(idea, ct);

        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ==========================================================
    // F1 — PUBLISH a draft
    // ==========================================================
    public async Task<bool> PublishAsync(Guid ideaId, Guid userId, CancellationToken ct = default)
    {
        var idea = await _db.Ideas.FirstOrDefaultAsync(i => i.Id == ideaId, ct);
        if (idea is null || idea.AuthorId != userId) return false;
        if (idea.IsPublished) return true;    // already live, nothing to do

        idea.IsPublished = true;
        idea.PublishedAt = DateTime.UtcNow;

        _db.ActivityLogs.Add(new ActivityLog
        {
            UserId = userId, ActivityType = "Idea",
            Description = $"Published \"{Format.Truncate(idea.Title, 60)}\"",
        });

        var author = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (author is not null) author.ReputationPoints += 10;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ==========================================================
    // F1 — DELETE (author only)
    // ==========================================================
    public async Task<bool> DeleteAsync(Guid ideaId, Guid userId, CancellationToken ct = default)
    {
        var idea = await _db.Ideas.FirstOrDefaultAsync(i => i.Id == ideaId, ct);
        if (idea is null || idea.AuthorId != userId) return false;

        _db.Ideas.Remove(idea);       // cascades to likes/bookmarks/comments
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ==========================================================
    // M5 — IDEA DETAIL
    // Increments the view counter, then returns everything the
    // detail page renders including AI results and comments.
    // ==========================================================
    public async Task<IdeaDetailDto?> GetByIdAsync(Guid ideaId, Guid viewerId, CancellationToken ct = default)
    {
        var idea = await _db.Ideas
            .Include(i => i.Author)
            .FirstOrDefaultAsync(i => i.Id == ideaId, ct);
        if (idea is null) return null;

        // Drafts are private to their author.
        if (!idea.IsPublished && idea.AuthorId != viewerId) return null;

        idea.Views++;
        await _db.SaveChangesAsync(ct);

        var comments = await _db.Comments
            .Include(c => c.Author)
            .Where(c => c.IdeaId == ideaId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

        return new IdeaDetailDto
        {
            Id = idea.Id,
            Title = idea.Title,
            Problem = idea.Problem,
            Solution = idea.Solution,
            Category = idea.Category,
            Tags = Format.SplitTags(idea.Tags),
            IsPublished = idea.IsPublished,
            Upvotes = idea.Upvotes,
            Views = idea.Views,
            CommentCount = idea.CommentCount,
            AuthorName = idea.Author?.FullName ?? "Unknown",
            AuthorId = idea.AuthorId,
            CreatedAt = idea.CreatedAt,
            IsMine = idea.AuthorId == viewerId,
            LikedByMe = await _db.IdeaLikes.AnyAsync(l => l.IdeaId == ideaId && l.UserId == viewerId, ct),
            BookmarkedByMe = await _db.IdeaBookmarks.AnyAsync(x => x.IdeaId == ideaId && x.UserId == viewerId, ct),
            AiAnalysis = idea.AiAnalysis,
            Swot = ParseSwot(idea.SwotJson),
            Comments = comments.Select(c => new CommentDto
            {
                Id = c.Id, Content = c.Content, ParentId = c.ParentId,
                AuthorName = c.Author?.FullName ?? "Unknown",
                TimeAgo = Format.TimeAgo(c.CreatedAt),
            }).ToList(),
        };
    }

    // ==========================================================
    // M5 — MY IDEAS (drafts included)
    // ==========================================================
    public async Task<List<IdeaCardDto>> GetMineAsync(Guid userId, CancellationToken ct = default)
    {
        var ideas = await _db.Ideas.Include(i => i.Author)
            .Where(i => i.AuthorId == userId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

        return ideas.Select(i => new IdeaCardDto
        {
            Id = i.Id, Title = i.Title, Summary = i.Summary, Category = i.Category,
            Tags = Format.SplitTags(i.Tags), Upvotes = i.Upvotes, Views = i.Views,
            CommentCount = i.CommentCount,
            AuthorName = i.Author?.FullName ?? "", AuthorRole = i.Author?.Role ?? "",
            CreatedAt = i.CreatedAt, TimeAgo = Format.TimeAgo(i.CreatedAt),
        }).ToList();
    }

    // ==========================================================
    // F2 — AI IDEA ANALYSIS
    // Asks Gemini for a structured critique and caches it on the row
    // so repeat visits do not burn quota.
    // ==========================================================
    public async Task<string?> AnalyzeAsync(Guid ideaId, Guid userId, CancellationToken ct = default)
    {
        var idea = await _db.Ideas.FirstOrDefaultAsync(i => i.Id == ideaId, ct);
        if (idea is null) return null;
        if (!idea.IsPublished && idea.AuthorId != userId) return null;

        // Cached result — return it rather than calling the model again.
        if (!string.IsNullOrWhiteSpace(idea.AiAnalysis)) return idea.AiAnalysis;

        var prompt = $"""
        You are an innovation analyst reviewing a submitted idea.

        Title    : {idea.Title}
        Category : {idea.Category}
        Problem  : {idea.Problem}
        Solution : {idea.Solution}

        Write a concise review in Markdown with exactly these four sections:
        ## Summary
        ## Strengths
        ## Gaps and Risks
        ## Suggested Next Steps

        Keep each section to 2-4 short bullet points or sentences.
        Be specific and practical. Do not invent facts about the market.
        """;

        var result = await _ai.GenerateTextAsync(prompt, ct);
        if (string.IsNullOrWhiteSpace(result)) return null;   // caller reports unavailability

        idea.AiAnalysis = result;
        idea.AiAnalysisAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return result;
    }

    // ==========================================================
    // F11 — AI SWOT ANALYSIS
    // Requests strict JSON so the UI can render four clean columns.
    // ==========================================================
    public async Task<SwotDto?> GenerateSwotAsync(Guid ideaId, Guid userId, CancellationToken ct = default)
    {
        var idea = await _db.Ideas.FirstOrDefaultAsync(i => i.Id == ideaId, ct);
        if (idea is null) return null;
        if (!idea.IsPublished && idea.AuthorId != userId) return null;

        var cached = ParseSwot(idea.SwotJson);
        if (cached is not null) return cached;

        // $$ raw string: with two '$' the interpolation syntax becomes
        // {{expr}}, leaving the single braces of the JSON example literal.
        var prompt = $$"""
        Perform a SWOT analysis of this innovation idea.

        Title    : {{idea.Title}}
        Category : {{idea.Category}}
        Problem  : {{idea.Problem}}
        Solution : {{idea.Solution}}

        Reply with ONLY a JSON object, no markdown fences, in exactly this shape:
        {"strengths":["..."],"weaknesses":["..."],"opportunities":["..."],"threats":["..."]}

        Give 3 short, specific entries per category.
        """;

        var raw = await _ai.GenerateTextAsync(prompt, ct);
        if (string.IsNullOrWhiteSpace(raw)) return null;

        try
        {
            // AiJson strips any markdown fence and tolerates a bad reply.
            var json = AiJson.Slice(raw);
            var swot = AiJson.Object<SwotDto>(raw);
            if (json is null || swot is null) return null;

            idea.SwotJson = json;
            idea.SwotAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return swot;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "SWOT JSON could not be parsed for idea {Id}", ideaId);
            return null;
        }
    }

    // ==========================================================
    // F3 — SIMILAR IDEA DETECTION
    // Compares this idea's embedding against every other published
    // idea and returns the closest matches above a threshold.
    // ==========================================================
    public async Task<List<SimilarIdeaDto>> FindSimilarAsync(Guid ideaId, CancellationToken ct = default)
    {
        var target = await _db.Ideas.FirstOrDefaultAsync(i => i.Id == ideaId, ct);
        if (target is null) return new();

        // Backfill: an idea created while the AI was unavailable has none.
        if (string.IsNullOrWhiteSpace(target.EmbeddingJson))
        {
            target.EmbeddingJson = await BuildEmbeddingAsync(target, ct);
            if (string.IsNullOrWhiteSpace(target.EmbeddingJson)) return new();
            await _db.SaveChangesAsync(ct);
        }

        var targetVec = SimilarityHelper.Deserialize(target.EmbeddingJson);
        if (targetVec.Length == 0) return new();

        var candidates = await _db.Ideas.Include(i => i.Author)
            .Where(i => i.Id != ideaId && i.IsPublished && i.EmbeddingJson != null)
            .ToListAsync(ct);

        return candidates
            .Select(i => new SimilarIdeaDto
            {
                Id = i.Id, Title = i.Title, Category = i.Category,
                AuthorName = i.Author?.FullName ?? "Unknown",
                Similarity = Math.Round(
                    SimilarityHelper.Cosine(targetVec, SimilarityHelper.Deserialize(i.EmbeddingJson)), 3),
            })
            // Threshold tuned against real Gemini embeddings: unrelated ideas
            // still score ~0.55 because the model's similarity floor is high
            // (a bicycle-logistics idea scored 0.56 against a solar-water one).
            // 0.70 cleanly separates genuinely related work from that noise.
            .Where(s => s.Similarity >= 0.70)
            .OrderByDescending(s => s.Similarity)
            .Take(5)
            .ToList();
    }

    // ==========================================================
    // F12 — AI BUSINESS MODEL GENERATOR  (M8)
    // Turns the idea into a structured business-model canvas.
    // Cached on the row like F2 and F11 so re-opening costs nothing.
    // ==========================================================
    public async Task<BusinessModelDto?> GenerateBusinessModelAsync(Guid ideaId, Guid userId,
                                                                    CancellationToken ct = default)
    {
        var idea = await _db.Ideas.FirstOrDefaultAsync(i => i.Id == ideaId, ct);
        if (idea is null) return null;
        if (!idea.IsPublished && idea.AuthorId != userId) return null;

        var cached = ParseBusinessModel(idea.BusinessModelJson);
        if (cached is not null) return cached;

        var prompt = $$"""
        Produce a business model canvas for this innovation idea.

        Title    : {{idea.Title}}
        Category : {{idea.Category}}
        Problem  : {{idea.Problem}}
        Solution : {{idea.Solution}}

        Reply with ONLY a JSON object, no markdown fences, in exactly this shape:
        {"valueProposition":"one or two sentences",
         "customerSegments":["..."],"revenueStreams":["..."],
         "keyResources":["..."],"keyPartners":["..."],
         "costStructure":["..."],"channels":["..."]}

        Give 2-4 short, specific entries in each array.
        """;

        var raw = await _ai.GenerateTextAsync(prompt, ct);
        if (string.IsNullOrWhiteSpace(raw)) return null;

        try
        {
            var json = AiJson.Slice(raw);
            var model = AiJson.Object<BusinessModelDto>(raw);
            if (json is null || model is null) return null;

            idea.BusinessModelJson = json;
            await _db.SaveChangesAsync(ct);
            return model;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Business model JSON could not be parsed for idea {Id}", ideaId);
            return null;
        }
    }

    private static BusinessModelDto? ParseBusinessModel(string? json) => AiJson.Object<BusinessModelDto>(json);

    // ---------- PRIVATE HELPERS ----------

    // Embeds title + problem + solution as one document.
    private async Task<string?> BuildEmbeddingAsync(Idea idea, CancellationToken ct)
    {
        var text = $"{idea.Title}. {idea.Problem} {idea.Solution}";
        var vector = await _ai.GenerateEmbeddingAsync(text, ct);
        // Null when the AI is unreachable — the idea still saves (NFR10).
        return vector is null ? null : SimilarityHelper.Serialize(vector);
    }

    private static SwotDto? ParseSwot(string? json) => AiJson.Object<SwotDto>(json);




}
