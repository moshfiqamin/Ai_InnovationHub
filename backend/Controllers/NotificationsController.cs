// ============================================================
// MODULE : M12 — Notifications
// LAYER  : Controller (MVC: C)
// FEATURE: F17 — Notification System
// ROUTES :
//   GET    /api/notifications?unreadOnly=       list
//   GET    /api/notifications/count             unread badge count
//   PUT    /api/notifications/{id}/read         mark one read
//   PUT    /api/notifications/read-all          mark all read
//   DELETE /api/notifications/{id}              dismiss
// ============================================================
using AiInnovationHub.Api.Models.DTOs;
using AiInnovationHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiInnovationHub.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : BaseApiController
{
    private readonly INotificationService _svc;
    public NotificationsController(INotificationService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] bool unreadOnly = false, CancellationToken ct = default)
    {
        return Ok(await _svc.GetAsync(UserId, unreadOnly, ct));
    }

    [HttpGet("count")]
    public async Task<IActionResult> Count(CancellationToken ct)
    {
        return Ok(new { unread = await _svc.UnreadCountAsync(UserId, ct) });
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        return await _svc.MarkReadAsync(id, UserId, ct)
            ? Ok(new { read = true })
            : Missing("Notification not found.");
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        return Ok(new { marked = await _svc.MarkAllReadAsync(UserId, ct) });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        return await _svc.DeleteAsync(id, UserId, ct)
            ? Ok(new { deleted = true })
            : Missing("Notification not found.");
    }
}
