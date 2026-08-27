// ============================================================
// MODULE : M14 — Administration
// LAYER  : Service
// FEATURE: F20 — Admin & AI Content Moderation (platform side)
// PURPOSE: Platform statistics and user management for administrators.
//          Report handling lives in ModerationService.
// ============================================================
using AiInnovationHub.Api.Data;
using AiInnovationHub.Api.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AiInnovationHub.Api.Services;

public interface IAdminService
{
    Task<AdminStatsDto> StatsAsync(CancellationToken ct = default);
    Task<List<AdminUserDto>> UsersAsync(string? search, CancellationToken ct = default);
    Task<(bool ok, string error)> SetRoleAsync(Guid targetUserId, Guid adminId, string role, CancellationToken ct = default);
}

public class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notify;

    // Every role an administrator may assign — the three privileged
    // ones the sign-up form deliberately withholds, plus the public set.
    public static readonly string[] AssignableRoles =
    {
        "Innovator", "Researcher", "Entrepreneur", "Mentor", "Investor",
        "Organization", "Judge", "Moderator", "Admin",
    };

    public AdminService(AppDbContext db, INotificationService notify)
    {
        _db = db; _notify = notify;
    }

    public async Task<AdminStatsDto> StatsAsync(CancellationToken ct = default)
    {
        var dto = new AdminStatsDto
        {
            TotalUsers       = await _db.Users.CountAsync(ct),
            TotalIdeas       = await _db.Ideas.CountAsync(ct),
            TotalProjects    = await _db.Projects.CountAsync(ct),
            TotalCommunities = await _db.Communities.CountAsync(ct),
            TotalChallenges  = await _db.Challenges.CountAsync(ct),
            PendingReports   = await _db.ContentReports.CountAsync(r => r.Status == "Pending", ct),
            FlaggedContent   = await _db.CommunityPosts.CountAsync(p => p.IsFlagged, ct),
        };

        var byRole = await _db.Users.GroupBy(u => u.Role)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count).ToListAsync(ct);

        foreach (var r in byRole)
        {
            dto.UsersByRole.Labels.Add(r.Role);
            dto.UsersByRole.Values.Add(r.Count);
        }

        // Sign-ups per day over the last week
        var today = DateTime.UtcNow.Date;
        for (int offset = 6; offset >= 0; offset--)
        {
            var day = today.AddDays(-offset);
            var next = day.AddDays(1);
            dto.SignupsOverTime.Labels.Add(day.ToString("ddd"));
            dto.SignupsOverTime.Values.Add(
                await _db.Users.CountAsync(u => u.CreatedAt >= day && u.CreatedAt < next, ct));
        }

        return dto;
    }

    public async Task<List<AdminUserDto>> UsersAsync(string? search, CancellationToken ct = default)
    {
        var q = _db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            q = q.Where(u => EF.Functions.ILike(u.FullName, term) || EF.Functions.ILike(u.Email, term));
        }

        var rows = await q.OrderByDescending(u => u.CreatedAt).Take(200).ToListAsync(ct);

        var result = new List<AdminUserDto>();
        foreach (var u in rows)
        {
            result.Add(new AdminUserDto
            {
                Id = u.Id, FullName = u.FullName, Email = u.Email, Role = u.Role,
                ReputationPoints = u.ReputationPoints,
                IdeaCount = await _db.Ideas.CountAsync(i => i.AuthorId == u.Id, ct),
                CreatedAt = u.CreatedAt,
            });
        }
        return result;
    }

    // ---- The ONLY path by which a privileged role can be granted ----
    public async Task<(bool, string)> SetRoleAsync(Guid targetUserId, Guid adminId, string role,
                                                   CancellationToken ct = default)
    {
        if (!AssignableRoles.Contains(role)) return (false, "That is not a valid role.");

        var target = await _db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId, ct);
        if (target is null) return (false, "That account does not exist.");

        // An admin must not demote themselves — doing so could leave the
        // platform with no administrator at all.
        if (targetUserId == adminId && role != "Admin")
            return (false, "You cannot change your own admin role.");

        target.Role = role;
        _notify.Push(targetUserId, "Moderation", $"An administrator set your role to {role}.", "/profile");
        await _db.SaveChangesAsync(ct);
        return (true, "");
    }
}
