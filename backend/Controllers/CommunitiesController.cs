// ============================================================
// MODULE : M7 — Community
// LAYER  : Controller (MVC: C)
// FEATURE: F5 — Community Discussion & Comments
// ROUTES :
//   GET    /api/communities                      list + discovery
//   GET    /api/communities/categories           filter options
//   POST   /api/communities                      create
//   GET    /api/communities/{id}                 detail
//   POST   /api/communities/{id}/join            join / leave
//   GET    /api/communities/{id}/members         member list
//   GET    /api/communities/{id}/posts           posts + comments
//   POST   /api/communities/{id}/posts           create a post
//   POST   /api/communities/posts/{postId}/upvote      react
//   POST   /api/communities/posts/{postId}/comments    comment/reply
// ============================================================
using AiInnovationHub.Api.Models.DTOs;
using AiInnovationHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiInnovationHub.Api.Controllers;

[ApiController]
[Route("api/communities")]
[Authorize]
public class CommunitiesController : BaseApiController
{
    private readonly ICommunityService _svc;
    public CommunitiesController(ICommunityService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? category, CancellationToken ct)
    {
        return Ok(await _svc.ListAsync(UserId, category, ct));
    }

    [HttpGet("categories")]
    public async Task<IActionResult> Categories(CancellationToken ct) => Ok(await _svc.CategoriesAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CommunityRequest req, CancellationToken ct)
    {
        var (id, error) = await _svc.CreateAsync(UserId, req, ct);
        return id is null ? Conflict(new ErrorResponse(error)) : Ok(new { id });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var dto = await _svc.GetAsync(id, UserId, ct);
        return dto is null ? Missing("That community does not exist.") : Ok(dto);
    }

    [HttpPost("{id:guid}/join")]
    public async Task<IActionResult> ToggleJoin(Guid id, CancellationToken ct)
    {
        return await _svc.ToggleJoinAsync(id, UserId, ct)
            ? Ok(new { toggled = true })
            : Missing("That community does not exist.");
    }

    [HttpGet("{id:guid}/members")]
    public async Task<IActionResult> Members(Guid id, CancellationToken ct) => Ok(await _svc.MembersAsync(id, ct));

    [HttpGet("{id:guid}/posts")]
    public async Task<IActionResult> Posts(Guid id, CancellationToken ct)
    {
        return Ok(await _svc.PostsAsync(id, UserId, ct));
    }

    [HttpPost("{id:guid}/posts")]
    public async Task<IActionResult> CreatePost(Guid id, [FromBody] PostRequest req, CancellationToken ct)
    {
        var (post, error) = await _svc.CreatePostAsync(id, UserId, req, ct);
        return post is null ? Fail(error) : Ok(post);
    }

    [HttpPost("posts/{postId:guid}/upvote")]
    public async Task<IActionResult> Upvote(Guid postId, CancellationToken ct)
    {
        var res = await _svc.TogglePostUpvoteAsync(postId, UserId, ct);
        return res is null ? Missing("That post does not exist.") : Ok(res);
    }

    [HttpPost("posts/{postId:guid}/comments")]
    public async Task<IActionResult> Comment(Guid postId, [FromBody] CommentRequest req, CancellationToken ct)
    {
        var c = await _svc.AddPostCommentAsync(postId, UserId, req, ct);
        return c is null ? Fail("Could not post that comment.") : Ok(c);
    }
}
