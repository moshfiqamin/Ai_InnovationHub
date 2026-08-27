// ============================================================
// MODULE : M4 — Innovation Feed
// LAYER  : Service — business logic behind FeedController
// FEATURE: F4 — Innovation Feed
// IMPLEMENTS (per requirements.pdf M4):
//   latest/trending feed · idea cards · like/upvote · comment entry
//   points · bookmark/save · share · feed filtering
// ============================================================
using AiInnovationHub.Api.Data;
using AiInnovationHub.Api.Models.DTOs;
using AiInnovationHub.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiInnovationHub.Api.Services;

public interface IFeedService
{
    Task<List<IdeaCardDto>> GetFeedAsync(Guid viewerId, string sort, string? category,
                                         string? search, CancellationToken ct = default);
    Task<List<string>> GetCategoriesAsync(CancellationToken ct = default);
    Task<ToggleResult?> ToggleLikeAsync(Guid ideaId, Guid userId, CancellationToken ct = default);
    Task<ToggleResult?> ToggleBookmarkAsync(Guid ideaId, Guid userId, CancellationToken ct = default);
    Task<List<IdeaCardDto>> GetBookmarksAsync(Guid userId, CancellationToken ct = default);
    Task<CommentDto?> AddCommentAsync(Guid ideaId, Guid userId, CommentRequest req, CancellationToken ct = default);
}

public class FeedService : IFeedService
{
    private readonly AppDbContext _db;
    public FeedService(AppDbContext db) => _db = db;

    // ==========================================================
    // F4 — THE FEED
    // sort: latest | trending | discussed
    // Optional category filter and free-text search.
    // ==========================================================
    public async Task<List<IdeaCardDto>> GetFeedAsync(Guid viewerId, string sort, string? category,
                                                      string? search, CancellationToken ct = default)
    {
        // Only published ideas ever reach the feed — drafts stay private.
        var q = _db.Ideas.Include(i => i.Author).Where(i => i.IsPublished);

        // ---- FILTER: category ----
        if (!string.IsNullOrWhiteSpace(category) && category != "All")
            q = q.Where(i => i.Category == category);

        // ---- FILTER: keyword search across title/problem/solution/tags ----
        // ILike is PostgreSQL's case-insensitive LIKE, so this stays a
        // database-side query rather than pulling every row into memory.
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            q = q.Where(i => EF.Functions.ILike(i.Title, term)
                          || EF.Functions.ILike(i.Problem, term)
                          || EF.Functions.ILike(i.Solution, term)
                          || EF.Functions.ILike(i.Tags, term));
        }

        // ---- SORT ----
        q = sort switch
        {
            "trending"  => q.OrderByDescending(i => i.Upvotes).ThenByDescending(i => i.PublishedAt),
            "discussed" => q.OrderByDescending(i => i.CommentCount).ThenByDescending(i => i.PublishedAt),
            _           => q.OrderByDescending(i => i.PublishedAt),   // "latest"
        };

        var ideas = await q.Take(50).ToListAsync(ct);
        var ideaIds = ideas.Select(i => i.Id).ToList();

        // Fetch this viewer's likes/bookmarks in two queries rather than
        // one per card (avoids the N+1 problem — NFR7 Performance).
        var myLikes = await _db.IdeaLikes
            .Where(l => l.UserId == viewerId && ideaIds.Contains(l.IdeaId))
            .Select(l => l.IdeaId).ToListAsync(ct);
        var myBookmarks = await _db.IdeaBookmarks
            .Where(x => x.UserId == viewerId && ideaIds.Contains(x.IdeaId))
            .Select(x => x.IdeaId).ToListAsync(ct);

        return ideas.Select(i => ToCard(i, myLikes.Contains(i.Id), myBookmarks.Contains(i.Id))).ToList();
    }

    // ---- Distinct categories, for the filter bar ----
    public async Task<List<string>> GetCategoriesAsync(CancellationToken ct = default) =>
        await _db.Ideas.Where(i => i.IsPublished && i.Category != "")
                       .Select(i => i.Category).Distinct().OrderBy(c => c).ToListAsync(ct);

    // ==========================================================
    // F4 — LIKE / UPVOTE (toggle)
    // The unique index on (UserId, IdeaId) means a second like is
    // impossible; this method turns that into an un-like instead.
    // ==========================================================
    public async Task<ToggleResult?> ToggleLikeAsync(Guid ideaId, Guid userId, CancellationToken ct = default)
    {
        var idea = await _db.Ideas.FirstOrDefaultAsync(i => i.Id == ideaId && i.IsPublished, ct);
        if (idea is null) return null;

        var existing = await _db.IdeaLikes
            .FirstOrDefaultAsync(l => l.IdeaId == ideaId && l.UserId == userId, ct);

        bool nowActive;
        if (existing is not null)
        {
            _db.IdeaLikes.Remove(existing);
            idea.Upvotes = Math.Max(0, idea.Upvotes - 1);   // never go negative
            nowActive = false;
        }
        else
        {
            _db.IdeaLikes.Add(new IdeaLike { IdeaId = ideaId, UserId = userId });
            idea.Upvotes++;
            nowActive = true;

            // The author gains reputation, but never from liking themselves.
            if (idea.AuthorId != userId)
            {
                var author = await _db.Users.FirstOrDefaultAsync(u => u.Id == idea.AuthorId, ct);
                if (author is not null) author.ReputationPoints += 2;
            }
        }

        await _db.SaveChangesAsync(ct);
        return new ToggleResult { Active = nowActive, Count = idea.Upvotes };
    }

    // ==========================================================
    // F4 — BOOKMARK / SAVE (toggle)
    // ==========================================================
    public async Task<ToggleResult?> ToggleBookmarkAsync(Guid ideaId, Guid userId, CancellationToken ct = default)
    {
        var exists = await _db.Ideas.AnyAsync(i => i.Id == ideaId && i.IsPublished, ct);
        if (!exists) return null;

        var existing = await _db.IdeaBookmarks
            .FirstOrDefaultAsync(x => x.IdeaId == ideaId && x.UserId == userId, ct);

        bool nowActive;
        if (existing is not null) { _db.IdeaBookmarks.Remove(existing); nowActive = false; }
        else { _db.IdeaBookmarks.Add(new IdeaBookmark { IdeaId = ideaId, UserId = userId }); nowActive = true; }

        await _db.SaveChangesAsync(ct);
        var total = await _db.IdeaBookmarks.CountAsync(x => x.UserId == userId, ct);
        return new ToggleResult { Active = nowActive, Count = total };
    }

    // ---- The viewer's saved ideas ----
    public async Task<List<IdeaCardDto>> GetBookmarksAsync(Guid userId, CancellationToken ct = default)
    {
        var ideas = await _db.IdeaBookmarks
            .Where(x => x.UserId == userId)
            .Include(x => x.Idea).ThenInclude(i => i!.Author)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.Idea!)
            .ToListAsync(ct);

        var likedIds = await _db.IdeaLikes.Where(l => l.UserId == userId)
                                          .Select(l => l.IdeaId).ToListAsync(ct);

        return ideas.Select(i => ToCard(i, likedIds.Contains(i.Id), true)).ToList();
    }

    // ==========================================================
    // F4 — ADD A COMMENT
    // Keeps Idea.CommentCount in step so the feed can sort by it
    // without a COUNT per row.
    // ==========================================================
    public async Task<CommentDto?> AddCommentAsync(Guid ideaId, Guid userId, CommentRequest req,
                                                   CancellationToken ct = default)
    {
        var idea = await _db.Ideas.FirstOrDefaultAsync(i => i.Id == ideaId && i.IsPublished, ct);
        if (idea is null) return null;

        // A reply must point at a comment that belongs to THIS idea,
        // otherwise threads could be grafted across ideas (NFR13).
        if (req.ParentId is not null)
        {
            var parentOk = await _db.Comments
                .AnyAsync(c => c.Id == req.ParentId && c.IdeaId == ideaId, ct);
            if (!parentOk) return null;
        }

        var comment = new Comment
        {
            IdeaId = ideaId, AuthorId = userId,
            Content = req.Content.Trim(), ParentId = req.ParentId,
        };

        _db.Comments.Add(comment);
        idea.CommentCount++;

        _db.ActivityLogs.Add(new ActivityLog
        {
            UserId = userId, ActivityType = "Community",
            Description = $"Commented on \"{Format.Truncate(idea.Title, 50)}\"",
        });

        await _db.SaveChangesAsync(ct);

        var author = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        return new CommentDto
        {
            Id = comment.Id, Content = comment.Content, ParentId = comment.ParentId,
            AuthorName = author?.FullName ?? "Unknown", TimeAgo = "just now",
        };
    }

    // ---- Entity -> feed card ----
    private static IdeaCardDto ToCard(Idea i, bool liked, bool bookmarked) => new()
    {
        Id = i.Id, Title = i.Title, Summary = i.Summary, Category = i.Category,
        Tags = Format.SplitTags(i.Tags),
        Upvotes = i.Upvotes, Views = i.Views, CommentCount = i.CommentCount,
        AuthorName = i.Author?.FullName ?? "Unknown", AuthorRole = i.Author?.Role ?? "",
        CreatedAt = i.CreatedAt, TimeAgo = Format.TimeAgo(i.PublishedAt ?? i.CreatedAt),
        LikedByMe = liked, BookmarkedByMe = bookmarked,
    };
}
