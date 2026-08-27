// ============================================================
// MODULE : M3 — Dashboard
// LAYER  : Controller (MVC: C)
// FEATURES:
//   F19 — Analytics Dashboard            GET /api/dashboard/summary
//   F18 — AI Personalized Recommendation GET /api/dashboard/recommendations
// PURPOSE: Both endpoints require a valid JWT. The controller only
//          resolves the caller's identity and delegates the real work
//          to DashboardService (MVC separation).
// ============================================================
using System.Security.Claims;
using AiInnovationHub.Api.Models.DTOs;
using AiInnovationHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiInnovationHub.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize] // every endpoint below needs a signed-in user (NFR2)
public class DashboardController : BaseApiController
{
    private readonly IDashboardService _dashboard;
    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    // ==========================================================
    // F19 — GET /api/dashboard/summary
    // Statistics, charts, trending ideas and recent activity.
    // ==========================================================
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var summary = await _dashboard.GetSummaryAsync(UserId, ct);
        return Ok(summary);
    }

    // ==========================================================
    // F18 — GET /api/dashboard/recommendations
    // AI-generated next actions. Never fails hard: the service
    // returns static fallbacks when Gemini is unavailable (NFR10).
    // ==========================================================
    [HttpGet("recommendations")]
    public async Task<IActionResult> GetRecommendations(CancellationToken ct)
    {
        var recs = await _dashboard.GetRecommendationsAsync(UserId, ct);
        return Ok(recs);
    }

}
