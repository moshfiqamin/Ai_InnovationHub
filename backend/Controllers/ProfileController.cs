// ============================================================
// MODULE : M13 — Profile
// LAYER  : Controller (MVC: C)
// FEATURE: F16 — Reputation & Badge System
// ROUTES :
//   GET /api/profile          my own profile
//   GET /api/profile/{id}     someone else's
//   PUT /api/profile          edit my own
// ============================================================
using AiInnovationHub.Api.Models.DTOs;
using AiInnovationHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiInnovationHub.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : BaseApiController
{
    private readonly IProfileService _svc;
    public ProfileController(IProfileService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        var dto = await _svc.GetAsync(UserId, UserId, ct);
        return dto is null ? Missing("Profile not found.") : Ok(dto);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var dto = await _svc.GetAsync(id, UserId, ct);
        return dto is null ? Missing("That profile does not exist.") : Ok(dto);
    }

    // A user may only ever edit their own profile — the id comes from the
    // JWT, never from the request body (NFR4).
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] ProfileUpdateRequest req, CancellationToken ct)
    {
        return await _svc.UpdateAsync(UserId, req, ct)
            ? Ok(new { updated = true })
            : Missing("Profile not found.");
    }
}
