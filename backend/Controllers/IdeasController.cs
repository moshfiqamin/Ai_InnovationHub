// ============================================================
// MODULE : M5 — Idea Management
// LAYER  : Controller (MVC: C)
// FEATURES: F1 Submission · F2 AI Analysis · F3 Similar Ideas · F11 SWOT
// ROUTES :
//   GET    /api/ideas/mine            my ideas (drafts included)
//   GET    /api/ideas/{id}            full detail
//   POST   /api/ideas                 create (draft or published)   F1
//   PUT    /api/ideas/{id}            update                        F1
//   POST   /api/ideas/{id}/publish    publish a draft               F1
//   DELETE /api/ideas/{id}            delete                        F1
//   POST   /api/ideas/{id}/analyze    AI analysis                   F2
//   POST   /api/ideas/{id}/swot       AI SWOT                       F11
//   GET    /api/ideas/{id}/similar    semantically similar ideas    F3
//   POST   /api/ideas/{id}/business-model  business model canvas   F12
// ============================================================
using AiInnovationHub.Api.Models.DTOs;
using AiInnovationHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiInnovationHub.Api.Controllers;

[ApiController]
[Route("api/ideas")]
[Authorize]
public class IdeasController : BaseApiController
{
    private readonly IIdeaService _ideas;
    public IdeasController(IIdeaService ideas) => _ideas = ideas;

    // ---- M5: my ideas, drafts included ----
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        return Ok(await _ideas.GetMineAsync(UserId, ct));
    }

    // ---- M5: full detail (drafts visible only to their author) ----
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var idea = await _ideas.GetByIdAsync(id, UserId, ct);
        return idea is null
            ? Missing("That idea does not exist, or is a private draft.")
            : Ok(idea);
    }

    // ---- F1: create ----
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] IdeaRequest req, CancellationToken ct)
    {
        var id = await _ideas.CreateAsync(UserId, req, ct);
        return Ok(new { id, published = req.Publish });
    }

    // ---- F1: update (author only) ----
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] IdeaRequest req, CancellationToken ct)
    {
        return await _ideas.UpdateAsync(id, UserId, req, ct)
            ? Ok(new { id })
            : Forbid();   // not the author
    }

    // ---- F1: publish a draft ----
    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        return await _ideas.PublishAsync(id, UserId, ct)
            ? Ok(new { id, published = true })
            : Missing("That idea does not exist, or is not yours.");
    }

    // ---- F1: delete ----
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        return await _ideas.DeleteAsync(id, UserId, ct)
            ? Ok(new { deleted = true })
            : Missing("That idea does not exist, or is not yours.");
    }

    // ---- F2: AI idea analysis ----
    [HttpPost("{id:guid}/analyze")]
    public async Task<IActionResult> Analyze(Guid id, CancellationToken ct)
    {
        var analysis = await _ideas.AnalyzeAsync(id, UserId, ct);
        // 503 rather than 500: the app is fine, the AI provider is not (NFR10).
        return analysis is null
            ? StatusCode(503, new ErrorResponse("AI analysis is unavailable right now. Please try again shortly."))
            : Ok(new { analysis });
    }

    // ---- F11: AI SWOT ----
    [HttpPost("{id:guid}/swot")]
    public async Task<IActionResult> Swot(Guid id, CancellationToken ct)
    {
        var swot = await _ideas.GenerateSwotAsync(id, UserId, ct);
        return swot is null
            ? StatusCode(503, new ErrorResponse("SWOT generation is unavailable right now. Please try again shortly."))
            : Ok(swot);
    }

    // ---- F12: AI business model generator (M8) ----
    [HttpPost("{id:guid}/business-model")]
    public async Task<IActionResult> BusinessModel(Guid id, CancellationToken ct)
    {
        var model = await _ideas.GenerateBusinessModelAsync(id, UserId, ct);
        return model is null
            ? StatusCode(503, new ErrorResponse("Business model generation is unavailable right now. Please try again shortly."))
            : Ok(model);
    }

    // ---- F3: similar ideas ----
    [HttpGet("{id:guid}/similar")]
    public async Task<IActionResult> Similar(Guid id, CancellationToken ct)
    {
        return Ok(await _ideas.FindSimilarAsync(id, ct));
    }
}
