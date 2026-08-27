// ============================================================
// MODULE : M9 — Innovation Challenges
// LAYER  : Controller (MVC: C)
// FEATURE: F14 — Innovation Challenges
// ROUTES :
//   GET  /api/challenges?status=            list
//   GET  /api/challenges/{id}               detail
//   POST /api/challenges                    create (Organization/Admin)
//   POST /api/challenges/{id}/submit        enter one of your ideas
//   GET  /api/challenges/{id}/submissions   leaderboard
//   PUT  /api/challenges/submissions/{id}/score   judge (organiser)
// ============================================================
using AiInnovationHub.Api.Models.DTOs;
using AiInnovationHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiInnovationHub.Api.Controllers;

[ApiController]
[Route("api/challenges")]
[Authorize]
public class ChallengesController : BaseApiController
{
    private readonly IChallengeService _svc;
    public ChallengesController(IChallengeService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct)
    {
        return Ok(await _svc.ListAsync(UserId, status, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var dto = await _svc.GetAsync(id, UserId, ct);
        return dto is null ? Missing("That challenge does not exist.") : Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ChallengeRequest req, CancellationToken ct)
    {
        var (id, error) = await _svc.CreateAsync(UserId, req, ct);
        // 403 not 400: the request was well formed, the caller just lacks the role.
        return id is null ? StatusCode(403, new ErrorResponse(error)) : Ok(new { id });
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, [FromBody] SubmissionRequest req, CancellationToken ct)
    {
        var (ok, error) = await _svc.SubmitAsync(id, UserId, req.IdeaId, ct);
        return ok ? Ok(new { submitted = true }) : Fail(error);
    }

    [HttpGet("{id:guid}/submissions")]
    public async Task<IActionResult> Submissions(Guid id, CancellationToken ct)
        => Ok(await _svc.SubmissionsAsync(id, ct));

    [HttpPut("submissions/{submissionId:guid}/score")]
    public async Task<IActionResult> Score(Guid submissionId, [FromBody] ScoreRequest req, CancellationToken ct)
    {
        var (ok, error) = await _svc.ScoreAsync(submissionId, UserId, req, ct);
        return ok ? Ok(new { scored = true }) : Fail(error);
    }
}
