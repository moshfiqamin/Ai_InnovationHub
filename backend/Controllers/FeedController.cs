// ============================================================
// MODULE : M4 — Innovation Feed
// LAYER  : Controller (MVC: C)
// FEATURE: F4 — Innovation Feed
// ROUTES :
//   GET    /api/feed?sort=&category=&search=   the feed itself
//   GET    /api/feed/categories                filter options
//   GET    /api/feed/bookmarks                 my saved ideas
//   POST   /api/feed/{ideaId}/like             toggle upvote
//   POST   /api/feed/{ideaId}/bookmark         toggle save
//   POST   /api/feed/{ideaId}/comments         add a comment
// ============================================================
using AiInnovationHub.Api.Models.DTOs;
using AiInnovationHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiInnovationHub.Api.Controllers;

[ApiController]
[Route("api/feed")]
[Authorize]
public class FeedController : BaseApiController
{
    private readonly IFeedService _feed;
    public FeedController(IFeedService feed) => _feed = feed;

    // ---- F4: the feed, with sorting, category filter and search ----
    [HttpGet]
    public async Task<IActionResult> GetFeed(
        [FromQuery] string sort = "latest",
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        return Ok(await _feed.GetFeedAsync(UserId, sort, category, search, ct));
    }

    // ---- F4: distinct categories for the filter bar ----
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
        => Ok(await _feed.GetCategoriesAsync(ct));

    // ---- F4: my bookmarked ideas ----
    [HttpGet("bookmarks")]
    public async Task<IActionResult> GetBookmarks(CancellationToken ct)
    {
        return Ok(await _feed.GetBookmarksAsync(UserId, ct));
    }

    // ---- F4: like / un-like ----
    [HttpPost("{ideaId:guid}/like")]
    public async Task<IActionResult> ToggleLike(Guid ideaId, CancellationToken ct)
    {
        var result = await _feed.ToggleLikeAsync(ideaId, UserId, ct);
        return result is null
            ? Missing("That idea does not exist or is not published.")
            : Ok(result);
    }

    // ---- F4: bookmark / un-bookmark ----
    [HttpPost("{ideaId:guid}/bookmark")]
    public async Task<IActionResult> ToggleBookmark(Guid ideaId, CancellationToken ct)
    {
        var result = await _feed.ToggleBookmarkAsync(ideaId, UserId, ct);
        return result is null
            ? Missing("That idea does not exist or is not published.")
            : Ok(result);
    }

    // ---- F4: comment on an idea ----
    [HttpPost("{ideaId:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid ideaId, [FromBody] CommentRequest req, CancellationToken ct)
    {
        var comment = await _feed.AddCommentAsync(ideaId, UserId, req, ct);
        return comment is null
            ? Fail("Could not post that comment.")
            : Ok(comment);
    }
}
