// ============================================================
// MODULE : M14 — Administration
// LAYER  : Controller (MVC: C)
// FEATURE: F20 — Admin & AI Content Moderation
// SECURITY: two different gates on purpose.
//   * User management and platform stats are Admin only.
//   * The moderation queue also admits Moderator, so an administrator can
//     delegate content review without granting full platform control.
//   * Reporting content sits outside both — any signed-in user may report.
// ROUTES :
//   GET  /api/admin/stats                 platform statistics
//   GET  /api/admin/users?search=         user management
//   PUT  /api/admin/users/{id}/role       grant a role
//   GET  /api/admin/reports?status=       moderation queue
//   PUT  /api/admin/reports/{id}/resolve  dismiss or remove
//   POST /api/admin/report                report content  (ANY user)
// ============================================================
using AiInnovationHub.Api.Models.DTOs;
using AiInnovationHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiInnovationHub.Api.Controllers;

[ApiController]
[Route("api/admin")]
// NOTE: only [Authorize] here, NOT [Authorize(Roles="Admin")].
// ASP.NET combines controller-level and method-level Authorize attributes
// with AND, so a controller-wide Roles="Admin" would override any wider
// method-level rule and lock Moderators out. Each action states its own.
[Authorize]
public class AdminController : BaseApiController
{
    private readonly IAdminService _admin;
    private readonly IModerationService _moderation;

    public AdminController(IAdminService admin, IModerationService moderation)
    {
        _admin = admin; _moderation = moderation;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("stats")]
    public async Task<IActionResult> Stats(CancellationToken ct) => Ok(await _admin.StatsAsync(ct));

    [Authorize(Roles = "Admin")]
    [HttpGet("users")]
    public async Task<IActionResult> Users([FromQuery] string? search, CancellationToken ct)
        => Ok(await _admin.UsersAsync(search, ct));

    [Authorize(Roles = "Admin")]
    [HttpPut("users/{id:guid}/role")]
    public async Task<IActionResult> SetRole(Guid id, [FromBody] AdminRoleRequest req, CancellationToken ct)
    {
        var (ok, error) = await _admin.SetRoleAsync(id, UserId, req.Role, ct);
        return ok ? Ok(new { role = req.Role }) : Fail(error);
    }

    // Overrides the controller-level gate: Moderators may review content.
    [Authorize(Roles = "Admin,Moderator")]
    [HttpGet("reports")]
    public async Task<IActionResult> Reports([FromQuery] string? status, CancellationToken ct)
        => Ok(await _moderation.QueueAsync(status, ct));

    // action: "dismiss" keeps the content, "remove" deletes it
    [Authorize(Roles = "Admin,Moderator")]
    [HttpPut("reports/{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id, [FromQuery] string action, CancellationToken ct)
    {
        if (action is not ("dismiss" or "remove"))
            return Fail("Action must be 'dismiss' or 'remove'.");

        return await _moderation.ResolveAsync(id, UserId, action, ct)
            ? Ok(new { resolved = action })
            : Missing("That report does not exist.");
    }
}

// ---- Reporting is open to every signed-in user, not just admins ----
[ApiController]
[Route("api/moderation")]
[Authorize]
public class ModerationController : BaseApiController
{
    private readonly IModerationService _moderation;
    public ModerationController(IModerationService moderation) => _moderation = moderation;

    [HttpPost("report")]
    public async Task<IActionResult> Report([FromBody] ReportRequest req, CancellationToken ct)
    {
        var (ok, error) = await _moderation.ReportAsync(UserId, req, ct);
        return ok ? Ok(new { reported = true }) : Fail(error);
    }
}
