// ============================================================
// MODULE : M8 — AI Intelligence
// LAYER  : Service
// FEATURE: F6 — AI Smart Search
// DESIGN : Hybrid. If the query can be embedded, ideas are ranked by
//          cosine similarity (true semantic search). If the embedding
//          provider is unavailable the search degrades to keyword
//          matching rather than returning nothing (NFR10).
//          Projects, people and communities are always keyword-matched
//          because they carry no embeddings.
// ============================================================
using AiInnovationHub.Api.Data;
using AiInnovationHub.Api.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AiInnovationHub.Api.Services;

public interface ISearchService
{
    Task<SearchResponse> SearchAsync(string query, Guid userId, CancellationToken ct = default);
    Task<int> BackfillEmbeddingsAsync(CancellationToken ct = default);
}

public class SearchService : ISearchService
{
    private readonly AppDbContext _db;
    private readonly IAiProvider _ai;

    public SearchService(AppDbContext db, IAiProvider ai) { _db = db; _ai = ai; }

    // ---- BACKFILL ----
    // Ideas inserted directly into the database (seed data) carry no
    // embedding, so F3 and F6 would silently skip them. This generates
    // the missing vectors on demand. Safe to call repeatedly.
    public async Task<int> BackfillEmbeddingsAsync(CancellationToken ct = default)
    {
        var missing = await _db.Ideas
            .Where(i => i.IsPublished && i.EmbeddingJson == null)
            .ToListAsync(ct);

        int done = 0;
        foreach (var idea in missing)
        {
            var vector = await _ai.GenerateEmbeddingAsync(
                $"{idea.Title}. {idea.Problem} {idea.Solution}", ct);
            if (vector is null) break;          // provider down: stop, retry later
            idea.EmbeddingJson = SimilarityHelper.Serialize(vector);
            done++;
        }

        if (done > 0) await _db.SaveChangesAsync(ct);
        return done;
    }

    public async Task<SearchResponse> SearchAsync(string query, Guid userId, CancellationToken ct = default)
    {
        var response = new SearchResponse { Query = query };
        if (string.IsNullOrWhiteSpace(query)) return response;

        var term = $"%{query.Trim()}%";
        var results = new List<SearchResultDto>();

        // ---- 1. IDEAS: semantic when possible ----
        var queryVector = await _ai.GenerateEmbeddingAsync(query, ct);

        if (queryVector is not null)
        {
            response.Mode = "semantic";
            var ideas = await _db.Ideas.Include(i => i.Author)
                .Where(i => i.IsPublished && i.EmbeddingJson != null)
                .ToListAsync(ct);

            results.AddRange(ideas
                .Select(i => new SearchResultDto
                {
                    Type = "Idea", Id = i.Id, Title = i.Title,
                    Subtitle = $"{i.Category} · {i.Author?.FullName}",
                    Link = $"/ideas/{i.Id}",
                    Score = Math.Round(SimilarityHelper.Cosine(
                        queryVector, SimilarityHelper.Deserialize(i.EmbeddingJson)), 3),
                })
                // NOTE: 0.55 here, NOT the 0.70 used by F3.
                // F3 compares one full idea against another (document to
                // document) and those score high. F6 compares a SHORT QUERY
                // against a full document, which scores systematically lower.
                // Measured against real Gemini embeddings:
                //   "clean water rural"  -> relevant 0.63, irrelevant 0.47
                //   "bicycle delivery"   -> relevant 0.69, irrelevant 0.44
                // 0.55 separates those cleanly; 0.70 wrongly dropped every one.
                .Where(r => r.Score >= 0.55)
                .OrderByDescending(r => r.Score)
                .Take(10));
        }
        else
        {
            response.Mode = "keyword";
            var ideas = await _db.Ideas.Include(i => i.Author)
                .Where(i => i.IsPublished &&
                    (EF.Functions.ILike(i.Title, term) ||
                     EF.Functions.ILike(i.Problem, term) ||
                     EF.Functions.ILike(i.Solution, term) ||
                     EF.Functions.ILike(i.Tags, term)))
                .Take(10).ToListAsync(ct);

            results.AddRange(ideas.Select(i => new SearchResultDto
            {
                Type = "Idea", Id = i.Id, Title = i.Title,
                Subtitle = $"{i.Category} · {i.Author?.FullName}",
                Link = $"/ideas/{i.Id}", Score = 1.0,
            }));
        }

        // ---- 2. PROJECTS the caller can actually see ----
        var myProjectIds = await _db.ProjectMembers
            .Where(m => m.UserId == userId && m.Status == "Active")
            .Select(m => m.ProjectId).ToListAsync(ct);

        var projects = await _db.Projects
            .Where(p => myProjectIds.Contains(p.Id) &&
                (EF.Functions.ILike(p.Title, term) || EF.Functions.ILike(p.Description, term)))
            .Take(5).ToListAsync(ct);

        results.AddRange(projects.Select(p => new SearchResultDto
        {
            Type = "Project", Id = p.Id, Title = p.Title,
            Subtitle = p.Status, Link = $"/projects/{p.Id}", Score = 0.9,
        }));

        // ---- 3. PEOPLE ----
        var users = await _db.Users
            .Where(u => EF.Functions.ILike(u.FullName, term)
                     || EF.Functions.ILike(u.Skills, term)
                     || EF.Functions.ILike(u.Expertise, term)
                     || EF.Functions.ILike(u.Headline, term))
            .Take(5).ToListAsync(ct);

        results.AddRange(users.Select(u => new SearchResultDto
        {
            Type = "User", Id = u.Id, Title = u.FullName,
            Subtitle = string.IsNullOrWhiteSpace(u.Headline) ? u.Role : u.Headline,
            Link = $"/profile/{u.Id}", Score = 0.85,
        }));

        // ---- 4. COMMUNITIES ----
        var communities = await _db.Communities
            .Where(c => EF.Functions.ILike(c.Name, term)
                     || EF.Functions.ILike(c.Description, term)
                     || EF.Functions.ILike(c.Category, term))
            .Take(5).ToListAsync(ct);

        results.AddRange(communities.Select(c => new SearchResultDto
        {
            Type = "Community", Id = c.Id, Title = c.Name,
            Subtitle = c.Category, Link = $"/communities/{c.Id}", Score = 0.8,
        }));

        response.Results = results.OrderByDescending(r => r.Score).Take(25).ToList();
        return response;
    }
}
