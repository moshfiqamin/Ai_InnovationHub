// ============================================================
// MODULE : M7 — Community
// LAYER  : Service
// FEATURE: F5 — Community Discussion & Comments
// IMPLEMENTS (per requirements.pdf M7): create/join communities,
//   community posts, comments and replies, upvotes/reactions,
//   member list, topic/category discovery.
// ============================================================
using AiInnovationHub.Api.Data;
using AiInnovationHub.Api.Models.DTOs;
using AiInnovationHub.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiInnovationHub.Api.Services;

public interface ICommunityService
{
    Task<List<CommunityDto>> ListAsync(Guid userId, string? category, CancellationToken ct = default);
    Task<(Guid? id, string error)> CreateAsync(Guid userId, CommunityRequest req, CancellationToken ct = default);
    Task<CommunityDto?> GetAsync(Guid communityId, Guid userId, CancellationToken ct = default);
    Task<bool> ToggleJoinAsync(Guid communityId, Guid userId, CancellationToken ct = default);
    Task<List<MemberDto>> MembersAsync(Guid communityId, CancellationToken ct = default);
    Task<List<PostDto>> PostsAsync(Guid communityId, Guid userId, CancellationToken ct = default);
    Task<(PostDto? post, string error)> CreatePostAsync(Guid communityId, Guid userId, PostRequest req, CancellationToken ct = default);
    Task<ToggleResult?> TogglePostUpvoteAsync(Guid postId, Guid userId, CancellationToken ct = default);
    Task<CommentDto?> AddPostCommentAsync(Guid postId, Guid userId, CommentRequest req, CancellationToken ct = default);
    Task<List<string>> CategoriesAsync(CancellationToken ct = default);
}

public class CommunityService : ICommunityService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notify;
    private readonly IBadgeService _badges;
    private readonly IModerationService _moderation;

    public CommunityService(AppDbContext db, INotificationService notify,
                            IBadgeService badges, IModerationService moderation)
    {
        _db = db; _notify = notify; _badges = badges; _moderation = moderation;
    }

    // ---- Topic/category discovery ----
    public async Task<List<CommunityDto>> ListAsync(Guid userId, string? category, CancellationToken ct = default)
    {
        var q = _db.Communities.AsQueryable();
        if (!string.IsNullOrWhiteSpace(category) && category != "All")
            q = q.Where(c => c.Category == category);

        var communities = await q.OrderBy(c => c.Name).ToListAsync(ct);
        var myIds = await _db.CommunityMembers.Where(m => m.UserId == userId)
                                              .Select(m => m.CommunityId).ToListAsync(ct);

        var result = new List<CommunityDto>();
        foreach (var c in communities)
        {
            result.Add(new CommunityDto
            {
                Id = c.Id, Name = c.Name, Description = c.Description, Category = c.Category,
                MemberCount = await _db.CommunityMembers.CountAsync(m => m.CommunityId == c.Id, ct),
                PostCount   = await _db.CommunityPosts.CountAsync(p => p.CommunityId == c.Id, ct),
                JoinedByMe  = myIds.Contains(c.Id),
            });
        }
        return result;
    }

    public Task<List<string>> CategoriesAsync(CancellationToken ct = default) =>
        _db.Communities.Where(c => c.Category != "").Select(c => c.Category)
                       .Distinct().OrderBy(c => c).ToListAsync(ct);

    // ---- Create a community; the creator joins automatically ----
    public async Task<(Guid?, string)> CreateAsync(Guid userId, CommunityRequest req, CancellationToken ct = default)
    {
        var name = req.Name.Trim();
        if (await _db.Communities.AnyAsync(c => c.Name == name, ct))
            return (null, "A community with that name already exists.");

        var community = new Community
        {
            Name = name, Description = req.Description.Trim(),
            Category = req.Category.Trim(), CreatedById = userId,
        };
        _db.Communities.Add(community);
        _db.CommunityMembers.Add(new CommunityMember { CommunityId = community.Id, UserId = userId });
        _db.ActivityLogs.Add(new ActivityLog
        {
            UserId = userId, ActivityType = "Community",
            Description = $"Created the community \"{Format.Truncate(name, 50)}\"",
        });

        await _db.SaveChangesAsync(ct);
        return (community.Id, "");
    }

    public async Task<CommunityDto?> GetAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var c = await _db.Communities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return null;

        return new CommunityDto
        {
            Id = c.Id, Name = c.Name, Description = c.Description, Category = c.Category,
            MemberCount = await _db.CommunityMembers.CountAsync(m => m.CommunityId == id, ct),
            PostCount   = await _db.CommunityPosts.CountAsync(p => p.CommunityId == id, ct),
            JoinedByMe  = await _db.CommunityMembers.AnyAsync(m => m.CommunityId == id && m.UserId == userId, ct),
        };
    }

    // ---- Join / leave ----
    public async Task<bool> ToggleJoinAsync(Guid communityId, Guid userId, CancellationToken ct = default)
    {
        if (!await _db.Communities.AnyAsync(c => c.Id == communityId, ct)) return false;

        var existing = await _db.CommunityMembers
            .FirstOrDefaultAsync(m => m.CommunityId == communityId && m.UserId == userId, ct);

        if (existing is not null) _db.CommunityMembers.Remove(existing);
        else _db.CommunityMembers.Add(new CommunityMember { CommunityId = communityId, UserId = userId });

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<MemberDto>> MembersAsync(Guid communityId, CancellationToken ct = default)
    {
        var rows = await _db.CommunityMembers.Include(m => m.User)
            .Where(m => m.CommunityId == communityId)
            .OrderBy(m => m.JoinedAt).Take(100).ToListAsync(ct);

        return rows.Select(m => new MemberDto
        {
            UserId = m.UserId, FullName = m.User?.FullName ?? "Unknown",
            Email = m.User?.Email ?? "", ProjectRole = m.User?.Role ?? "", Status = "Active",
        }).ToList();
    }

    // ---- Posts with their comments ----
    public async Task<List<PostDto>> PostsAsync(Guid communityId, Guid userId, CancellationToken ct = default)
    {
        var posts = await _db.CommunityPosts.Include(p => p.Author)
            .Where(p => p.CommunityId == communityId)
            .OrderByDescending(p => p.CreatedAt).Take(50).ToListAsync(ct);

        var postIds = posts.Select(p => p.Id).ToList();
        var myUpvotes = await _db.PostUpvotes
            .Where(u => u.UserId == userId && postIds.Contains(u.PostId))
            .Select(u => u.PostId).ToListAsync(ct);

        var comments = await _db.PostComments.Include(c => c.Author)
            .Where(c => postIds.Contains(c.PostId))
            .OrderBy(c => c.CreatedAt).ToListAsync(ct);

        return posts.Select(p => new PostDto
        {
            Id = p.Id, Title = p.Title, Content = p.Content,
            AuthorName = p.Author?.FullName ?? "Unknown",
            Upvotes = p.Upvotes, CommentCount = p.CommentCount,
            UpvotedByMe = myUpvotes.Contains(p.Id),
            IsFlagged = p.IsFlagged,
            TimeAgo = Format.TimeAgo(p.CreatedAt),
            Comments = comments.Where(c => c.PostId == p.Id).Select(c => new CommentDto
            {
                Id = c.Id, Content = c.Content, ParentId = c.ParentId,
                AuthorName = c.Author?.FullName ?? "Unknown",
                TimeAgo = Format.TimeAgo(c.CreatedAt),
            }).ToList(),
        }).ToList();
    }

    // ---- Create a post (members only) ----
    public async Task<(PostDto?, string)> CreatePostAsync(Guid communityId, Guid userId, PostRequest req,
                                                          CancellationToken ct = default)
    {
        var isMember = await _db.CommunityMembers
            .AnyAsync(m => m.CommunityId == communityId && m.UserId == userId, ct);
        if (!isMember) return (null, "Join this community before posting.");

        var post = new CommunityPost
        {
            CommunityId = communityId, AuthorId = userId,
            Title = req.Title.Trim(), Content = req.Content.Trim(),
        };
        _db.CommunityPosts.Add(post);
        _db.ActivityLogs.Add(new ActivityLog
        {
            UserId = userId, ActivityType = "Community",
            Description = $"Posted \"{Format.Truncate(post.Title, 50)}\"",
        });
        await _db.SaveChangesAsync(ct);

        // F20: run AI moderation in the background of this request. It
        // may set IsFlagged and open a report for M14 to review.
        await _moderation.ScreenPostAsync(post.Id, ct);

        var author = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        return (new PostDto
        {
            Id = post.Id, Title = post.Title, Content = post.Content,
            AuthorName = author?.FullName ?? "Unknown",
            Upvotes = 0, CommentCount = 0, TimeAgo = "just now",
        }, "");
    }

    // ---- Upvote a post ----
    public async Task<ToggleResult?> TogglePostUpvoteAsync(Guid postId, Guid userId, CancellationToken ct = default)
    {
        var post = await _db.CommunityPosts.FirstOrDefaultAsync(p => p.Id == postId, ct);
        if (post is null) return null;

        var existing = await _db.PostUpvotes.FirstOrDefaultAsync(u => u.PostId == postId && u.UserId == userId, ct);
        bool active;
        if (existing is not null)
        {
            _db.PostUpvotes.Remove(existing);
            post.Upvotes = Math.Max(0, post.Upvotes - 1);
            active = false;
        }
        else
        {
            _db.PostUpvotes.Add(new PostUpvote { PostId = postId, UserId = userId });
            post.Upvotes++;
            active = true;

            if (post.AuthorId != userId)
            {
                var author = await _db.Users.FirstOrDefaultAsync(u => u.Id == post.AuthorId, ct);
                if (author is not null) author.ReputationPoints += 1;
                _notify.Push(post.AuthorId, "Like",
                    $"Your post \"{Format.Truncate(post.Title, 40)}\" was upvoted.",
                    $"/communities/{post.CommunityId}");
            }
        }

        await _db.SaveChangesAsync(ct);
        return new ToggleResult { Active = active, Count = post.Upvotes };
    }

    // ---- Comment / reply on a post ----
    public async Task<CommentDto?> AddPostCommentAsync(Guid postId, Guid userId, CommentRequest req,
                                                       CancellationToken ct = default)
    {
        var post = await _db.CommunityPosts.FirstOrDefaultAsync(p => p.Id == postId, ct);
        if (post is null) return null;

        // A reply must belong to the same post (NFR13).
        if (req.ParentId is not null &&
            !await _db.PostComments.AnyAsync(c => c.Id == req.ParentId && c.PostId == postId, ct))
            return null;

        var comment = new PostComment
        {
            PostId = postId, AuthorId = userId,
            Content = req.Content.Trim(), ParentId = req.ParentId,
        };
        _db.PostComments.Add(comment);
        post.CommentCount++;

        if (post.AuthorId != userId)
            _notify.Push(post.AuthorId, "Comment",
                $"New reply on \"{Format.Truncate(post.Title, 40)}\".",
                $"/communities/{post.CommunityId}");

        await _db.SaveChangesAsync(ct);
        await _badges.AwardNewAsync(userId, ct);

        var author = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        return new CommentDto
        {
            Id = comment.Id, Content = comment.Content, ParentId = comment.ParentId,
            AuthorName = author?.FullName ?? "Unknown", TimeAgo = "just now",
        };
    }
}
