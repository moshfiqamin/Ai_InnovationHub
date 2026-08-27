// ============================================================
// MODULE : M11 — Analytics
// LAYER  : Controller (MVC: C)
// FEATURE: F19 — Analytics Dashboard (detailed view)
// ROUTE  : GET /api/analytics
// ============================================================
using AiInnovationHub.Api.Models.DTOs;
using AiInnovationHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiInnovationHub.Api.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController : BaseApiController
{
    private readonly IAnalyticsService _svc;
    public AnalyticsController(IAnalyticsService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        return Ok(await _svc.GetAsync(UserId, ct));
    }
}
