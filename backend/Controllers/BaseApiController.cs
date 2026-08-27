// ============================================================
// FILE   : Controllers/BaseApiController.cs
// LAYER  : Controller (MVC: C) — shared base for every controller
// PURPOSE: Removes the identity boilerplate that was repeated in all
//          64 controller actions.
// WHY IT IS SAFE: every controller carries [Authorize], so ASP.NET
//   rejects a missing or invalid token BEFORE any action runs. By the
//   time our code executes, a valid user id is guaranteed — the old
//   per-action null check could never actually fire.
// ============================================================
using System.Security.Claims;
using AiInnovationHub.Api.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace AiInnovationHub.Api.Controllers;

public abstract class BaseApiController : ControllerBase
{
    // The signed-in user's id, read from the JWT claim TokenService writes.
    protected Guid UserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new UnauthorizedAccessException("No valid user id in token.");

    // ---- SHARED RESPONSE SHAPES ----
    // Every controller returned the same two shapes; these name them once.
    protected IActionResult Fail(string message) => BadRequest(new ErrorResponse(message));
    protected IActionResult Missing(string message) => NotFound(new ErrorResponse(message));

    // Turns a (ok, error) service result into the right HTTP response.
    protected IActionResult Result((bool ok, string error) r, object? payload = null) =>
        r.ok ? Ok(payload ?? new { ok = true }) : Fail(r.error);
}
