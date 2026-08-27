// ============================================================
// MODULE : M10 — Mentor & Investor
// LAYER  : Controller (MVC: C)
// FEATURES: F13 AI Mentor Recommendation · F15 Investor Connect
// ROUTES :
//   GET  /api/mentors?search=             mentor directory
//   GET  /api/mentors/recommended         AI suggestions        F13
//   POST /api/mentors/{id}/request        request mentorship    F13
//   GET  /api/investors?search=           investor directory    F15
//   POST /api/investors/interest          register interest     F15
//   GET  /api/engagements                 my requests, both ways
//   PUT  /api/engagements/{kind}/{id}     accept / decline
// ============================================================
using AiInnovationHub.Api.Models.DTOs;
using AiInnovationHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiInnovationHub.Api.Controllers;

[ApiController]
[Authorize]
public class EngagementController : BaseApiController
{
    private readonly IEngagementService _svc;
    public EngagementController(IEngagementService svc) => _svc = svc;

    // ---- F13: mentors ----
    [HttpGet("api/mentors")]
    public async Task<IActionResult> Mentors([FromQuery] string? search, CancellationToken ct)
        => Ok(await _svc.ListMentorsAsync(search, ct));

    [HttpGet("api/mentors/recommended")]
    public async Task<IActionResult> Recommended(CancellationToken ct)
    {
        return Ok(await _svc.RecommendMentorsAsync(UserId, ct));
    }

    [HttpPost("api/mentors/{mentorId:guid}/request")]
    public async Task<IActionResult> RequestMentorship(Guid mentorId,
        [FromBody] MentorshipRequestDto req, CancellationToken ct)
    {
        var (ok, error) = await _svc.RequestMentorshipAsync(mentorId, UserId, req.Message, ct);
        return ok ? Ok(new { requested = true }) : Fail(error);
    }

    // ---- F15: investors ----
    [HttpGet("api/investors")]
    public async Task<IActionResult> Investors([FromQuery] string? search, CancellationToken ct)
        => Ok(await _svc.ListInvestorsAsync(search, ct));

    [HttpPost("api/investors/interest")]
    public async Task<IActionResult> Interest([FromBody] InvestmentRequestDto req, CancellationToken ct)
    {
        var (ok, error) = await _svc.ExpressInterestAsync(UserId, req, ct);
        return ok ? Ok(new { registered = true }) : Fail(error);
    }

    // ---- Both kinds of engagement in one list ----
    [HttpGet("api/engagements")]
    public async Task<IActionResult> MyEngagements(CancellationToken ct)
    {
        return Ok(await _svc.MyEngagementsAsync(UserId, ct));
    }

    // kind: "mentorship" | "investment"
    [HttpPut("api/engagements/{kind}/{id:guid}")]
    public async Task<IActionResult> Respond(string kind, Guid id,
        [FromBody] StatusRequest req, CancellationToken ct)
    {
        var (ok, error) = await _svc.RespondAsync(id, UserId, kind, req.Status, ct);
        return ok ? Ok(new { status = req.Status }) : Fail(error);
    }
}
