// ============================================================
// MODULE : M12 — Notifications
// LAYER  : Service
// FEATURE: F17 — Notification System
// PURPOSE: Central place to raise alerts. Every other module calls
//          PushAsync rather than writing Notification rows itself, so
//          the wording and dedupe rules stay in one place (NFR8).
// ============================================================
using AiInnovationHub.Api.Data;
using AiInnovationHub.Api.Models.DTOs;
using AiInnovationHub.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiInnovationHub.Api.Services;

public interface INotificationService
{
    // Queues a notification. Does NOT save — the caller's SaveChanges
    // commits it in the same transaction as whatever triggered it.
    void Push(Guid userId, string type, string message, string? link = null);
    Task<List<NotificationDto>> GetAsync(Guid userId, bool unreadOnly, CancellationToken ct = default);
    Task<int> UnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task<bool> MarkReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default);
    Task<int> MarkAllReadAsync(Guid userId, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid notificationId, Guid userId, CancellationToken ct = default);
}

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    public NotificationService(AppDbContext db) => _db = db;

    public void Push(Guid userId, string type, string message, string? link = null)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId, Type = type,
            Message = message.Length > 400 ? message[..400] : message,
            Link = link,
        });
    }

    public async Task<List<NotificationDto>> GetAsync(Guid userId, bool unreadOnly, CancellationToken ct = default)
    {
        var q = _db.Notifications.Where(n => n.UserId == userId);
        if (unreadOnly) q = q.Where(n => !n.IsRead);

        var rows = await q.OrderByDescending(n => n.CreatedAt).Take(50).ToListAsync(ct);

        return rows.Select(n => new NotificationDto
        {
            Id = n.Id, Type = n.Type, Message = n.Message, Link = n.Link,
            IsRead = n.IsRead, TimeAgo = Format.TimeAgo(n.CreatedAt),
        }).ToList();
    }

    public Task<int> UnreadCountAsync(Guid userId, CancellationToken ct = default) =>
        _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task<bool> MarkReadAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        // Scoped by UserId so one user cannot mark another's alerts read.
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (n is null) return false;
        n.IsRead = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> MarkAllReadAsync(Guid userId, CancellationToken ct = default)
    {
        var unread = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync(ct);
        unread.ForEach(n => n.IsRead = true);
        await _db.SaveChangesAsync(ct);
        return unread.Count;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (n is null) return false;
        _db.Notifications.Remove(n);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
