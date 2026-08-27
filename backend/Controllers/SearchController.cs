// ============================================================
// MODULE : M8 — AI Intelligence
// LAYER  : Controller (MVC: C)
// FEATURE: F6 — AI Smart Search
// ROUTE  : GET /api/search?q=...
// ============================================================
using AiInnovationHub.Api.Models.DTOs;
using AiInnovationHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiInnovationHub.Api.Controllers;

[ApiController]
[Route("api/search")]
[Authorize]
public class SearchController : BaseApiController
{
    private readonly ISearchService _search;
    public SearchController(ISearchService search) => _search = search;

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new SearchResponse { Query = q ?? "" });

        return Ok(await _search.SearchAsync(q, UserId, ct));
    }

    // ---- Generate embeddings for ideas that have none ----
    // Needed for seed data inserted straight into the database, and to
    // recover ideas created while the AI provider was unavailable.
    [HttpPost("backfill")]
    public async Task<IActionResult> Backfill(CancellationToken ct)
    {
        var count = await _search.BackfillEmbeddingsAsync(ct);
        return Ok(new { embedded = count });
    }
}
