// ============================================================
// MODULE : M6 — Project Collaboration
// LAYER  : Service — business logic behind ProjectsController
// FEATURES:
//   F7  Team Formation        (Invite / Accept / ChangeRole / Remove)
//   F8  Project Workspace     (Create / GetWorkspace / Milestones)
//   F9  Task Management       (Create / Update / SetStatus / Delete)
//   F10 File & Resource Share (Upload / List / Download / Delete)
// AUTHORISATION MODEL:
//   Owner      — everything, including deleting the project
//   Maintainer — manage members, tasks, milestones, files
//   Contributor— create/update tasks, upload files
//   Viewer     — read only
// ============================================================
using AiInnovationHub.Api.Data;
using AiInnovationHub.Api.Models.DTOs;
using AiInnovationHub.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiInnovationHub.Api.Services;

public interface IProjectService
{
    Task<List<ProjectCardDto>> GetMyProjectsAsync(Guid userId, CancellationToken ct = default);
    Task<Guid?> CreateAsync(Guid ownerId, ProjectRequest req, CancellationToken ct = default);
    Task<ProjectWorkspaceDto?> GetWorkspaceAsync(Guid projectId, Guid userId, CancellationToken ct = default);

    Task<(bool ok, string error)> InviteAsync(Guid projectId, Guid actorId, InviteRequest req, CancellationToken ct = default);
    Task<bool> AcceptInviteAsync(Guid projectId, Guid userId, CancellationToken ct = default);
    Task<(bool ok, string error)> ChangeMemberRoleAsync(Guid projectId, Guid actorId, Guid memberUserId, string role, CancellationToken ct = default);
    Task<(bool ok, string error)> RemoveMemberAsync(Guid projectId, Guid actorId, Guid memberUserId, CancellationToken ct = default);

    Task<TaskDto?> CreateTaskAsync(Guid projectId, Guid actorId, TaskRequest req, CancellationToken ct = default);
    Task<bool> SetTaskStatusAsync(Guid taskId, Guid actorId, string status, CancellationToken ct = default);
    Task<bool> DeleteTaskAsync(Guid taskId, Guid actorId, CancellationToken ct = default);

    Task<MilestoneDto?> CreateMilestoneAsync(Guid projectId, Guid actorId, MilestoneRequest req, CancellationToken ct = default);
    Task<bool> ToggleMilestoneAsync(Guid milestoneId, Guid actorId, CancellationToken ct = default);

    Task<ProjectFileDto?> AddFileAsync(Guid projectId, Guid actorId, string fileName, string storedName,
                                       string contentType, long size, CancellationToken ct = default);
    Task<ProjectFile?> GetFileAsync(Guid fileId, Guid actorId, CancellationToken ct = default);
    Task<bool> DeleteFileAsync(Guid fileId, Guid actorId, CancellationToken ct = default);
}

public class ProjectService : IProjectService
{
    private readonly AppDbContext _db;
    public ProjectService(AppDbContext db) => _db = db;

    // Roles allowed to administer a project
    private static readonly string[] ManagerRoles = { "Owner", "Maintainer" };
    // Roles allowed to contribute content
    private static readonly string[] ContributorRoles = { "Owner", "Maintainer", "Contributor" };

    // ---- MEMBERSHIP LOOKUP ----
    // Returns the caller's ACTIVE membership, or null if they are not on
    // the team. Every method below gates on this, so a stranger cannot
    // read or modify a project by guessing its id (NFR2).
    private Task<ProjectMember?> MembershipAsync(Guid projectId, Guid userId, CancellationToken ct) =>
        _db.ProjectMembers.FirstOrDefaultAsync(
            m => m.ProjectId == projectId && m.UserId == userId && m.Status == "Active", ct);

    // ==========================================================
    // F8 — CREATE A PROJECT
    // The creator is automatically added as an Active Owner.
    // ==========================================================
    public async Task<Guid?> CreateAsync(Guid ownerId, ProjectRequest req, CancellationToken ct = default)
    {
        // If promoting from an idea, the caller must own that idea.
        if (req.SourceIdeaId is not null)
        {
            var ownsIdea = await _db.Ideas
                .AnyAsync(i => i.Id == req.SourceIdeaId && i.AuthorId == ownerId, ct);
            if (!ownsIdea) return null;
        }

        var project = new Project
        {
            OwnerId = ownerId,
            Title = req.Title.Trim(),
            Description = req.Description.Trim(),
            SourceIdeaId = req.SourceIdeaId,
            Status = "Planning",
        };

        _db.Projects.Add(project);
        _db.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = project.Id, UserId = ownerId,
            ProjectRole = "Owner", Status = "Active",
        });
        _db.ActivityLogs.Add(new ActivityLog
        {
            UserId = ownerId, ActivityType = "Project",
            Description = $"Created project \"{Format.Truncate(project.Title, 60)}\"",
        });

        await _db.SaveChangesAsync(ct);
        return project.Id;
    }

    // ==========================================================
    // F8 — PROJECTS I BELONG TO
    // ==========================================================
    public async Task<List<ProjectCardDto>> GetMyProjectsAsync(Guid userId, CancellationToken ct = default)
    {
        var memberships = await _db.ProjectMembers
            .Where(m => m.UserId == userId)
            .Include(m => m.Project).ThenInclude(p => p!.Owner)
            .ToListAsync(ct);

        var result = new List<ProjectCardDto>();
        foreach (var m in memberships)
        {
            if (m.Project is null) continue;
            var taskCount = await _db.ProjectTasks.CountAsync(t => t.ProjectId == m.ProjectId, ct);
            var doneCount = await _db.ProjectTasks.CountAsync(t => t.ProjectId == m.ProjectId && t.Status == "Done", ct);
            var memberCount = await _db.ProjectMembers.CountAsync(x => x.ProjectId == m.ProjectId && x.Status == "Active", ct);

            result.Add(new ProjectCardDto
            {
                Id = m.Project.Id, Title = m.Project.Title, Description = m.Project.Description,
                Status = m.Project.Status, OwnerName = m.Project.Owner?.FullName ?? "Unknown",
                MemberCount = memberCount, TaskCount = taskCount, CompletedTaskCount = doneCount,
                // "Invited" surfaces a pending invitation in the UI (F7).
                MyRole = m.Status == "Active" ? m.ProjectRole : "Invited",
                CreatedAt = m.Project.CreatedAt,
            });
        }
        return result.OrderByDescending(p => p.CreatedAt).ToList();
    }

    // ==========================================================
    // F8 — THE WORKSPACE (members + tasks + milestones + files)
    // ==========================================================
    public async Task<ProjectWorkspaceDto?> GetWorkspaceAsync(Guid projectId, Guid userId, CancellationToken ct = default)
    {
        var me = await MembershipAsync(projectId, userId, ct);
        if (me is null) return null;    // not a member -> 404 to the caller

        var project = await _db.Projects
            .Include(p => p.Owner).Include(p => p.SourceIdea)
            .FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (project is null) return null;

        var members = await _db.ProjectMembers.Include(m => m.User)
            .Where(m => m.ProjectId == projectId).ToListAsync(ct);

        var tasks = await _db.ProjectTasks.Include(t => t.Assignee)
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.Status == "Done").ThenByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        var milestones = await _db.Milestones
            .Where(m => m.ProjectId == projectId).OrderBy(m => m.DueDate).ToListAsync(ct);

        var files = await _db.ProjectFiles.Include(f => f.UploadedBy)
            .Where(f => f.ProjectId == projectId).OrderByDescending(f => f.UploadedAt).ToListAsync(ct);

        return new ProjectWorkspaceDto
        {
            Id = project.Id, Title = project.Title, Description = project.Description,
            Status = project.Status, OwnerName = project.Owner?.FullName ?? "Unknown",
            OwnerId = project.OwnerId,
            SourceIdeaId = project.SourceIdeaId, SourceIdeaTitle = project.SourceIdea?.Title,
            MyRole = me.ProjectRole,
            CanManage = ManagerRoles.Contains(me.ProjectRole),

            Members = members.Select(m => new MemberDto
            {
                UserId = m.UserId, FullName = m.User?.FullName ?? "Unknown",
                Email = m.User?.Email ?? "", ProjectRole = m.ProjectRole, Status = m.Status,
            }).ToList(),

            Tasks = tasks.Select(ToTaskDto).ToList(),

            Milestones = milestones.Select(m => new MilestoneDto
            {
                Id = m.Id, Title = m.Title, DueDate = m.DueDate, IsCompleted = m.IsCompleted,
            }).ToList(),

            Files = files.Select(f => new ProjectFileDto
            {
                Id = f.Id, FileName = f.FileName, ContentType = f.ContentType,
                SizeBytes = f.SizeBytes, SizeLabel = Format.FileSize(f.SizeBytes),
                UploadedByName = f.UploadedBy?.FullName ?? "Unknown", UploadedAt = f.UploadedAt,
            }).ToList(),
        };
    }

    // ==========================================================
    // F7 — INVITE A MEMBER
    // ==========================================================
    public async Task<(bool ok, string error)> InviteAsync(Guid projectId, Guid actorId, InviteRequest req,
                                                           CancellationToken ct = default)
    {
        var me = await MembershipAsync(projectId, actorId, ct);
        if (me is null) return (false, "You are not a member of this project.");
        if (!ManagerRoles.Contains(me.ProjectRole))
            return (false, "Only the owner or a maintainer can invite members.");

        var email = req.Email.Trim().ToLowerInvariant();
        var invitee = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (invitee is null) return (false, "No account exists with that email address.");

        if (await _db.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == invitee.Id, ct))
            return (false, "That person is already on this project.");

        // Owner is never assignable through an invite.
        var role = new[] { "Maintainer", "Contributor", "Viewer" }.Contains(req.ProjectRole)
            ? req.ProjectRole : "Contributor";

        _db.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = projectId, UserId = invitee.Id,
            ProjectRole = role, Status = "Invited",
        });
        _db.ActivityLogs.Add(new ActivityLog
        {
            UserId = invitee.Id, ActivityType = "Project",
            Description = "You were invited to a project",
        });

        await _db.SaveChangesAsync(ct);
        return (true, "");
    }

    // ---- F7: the invitee accepts ----
    public async Task<bool> AcceptInviteAsync(Guid projectId, Guid userId, CancellationToken ct = default)
    {
        var invite = await _db.ProjectMembers.FirstOrDefaultAsync(
            m => m.ProjectId == projectId && m.UserId == userId && m.Status == "Invited", ct);
        if (invite is null) return false;

        invite.Status = "Active";
        invite.JoinedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ---- F7: change a member's project role ----
    public async Task<(bool ok, string error)> ChangeMemberRoleAsync(Guid projectId, Guid actorId,
        Guid memberUserId, string role, CancellationToken ct = default)
    {
        var me = await MembershipAsync(projectId, actorId, ct);
        if (me is null || !ManagerRoles.Contains(me.ProjectRole))
            return (false, "Only the owner or a maintainer can change roles.");

        if (!new[] { "Maintainer", "Contributor", "Viewer" }.Contains(role))
            return (false, "That is not a valid project role.");

        var target = await _db.ProjectMembers
            .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == memberUserId, ct);
        if (target is null) return (false, "That person is not on this project.");

        // The owner's role is immutable — otherwise a maintainer could
        // demote the owner and take over the project.
        if (target.ProjectRole == "Owner") return (false, "The project owner's role cannot be changed.");

        target.ProjectRole = role;
        await _db.SaveChangesAsync(ct);
        return (true, "");
    }

    // ---- F7: remove a member ----
    public async Task<(bool ok, string error)> RemoveMemberAsync(Guid projectId, Guid actorId,
        Guid memberUserId, CancellationToken ct = default)
    {
        var me = await MembershipAsync(projectId, actorId, ct);
        if (me is null || !ManagerRoles.Contains(me.ProjectRole))
            return (false, "Only the owner or a maintainer can remove members.");

        var target = await _db.ProjectMembers
            .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == memberUserId, ct);
        if (target is null) return (false, "That person is not on this project.");
        if (target.ProjectRole == "Owner") return (false, "The project owner cannot be removed.");

        _db.ProjectMembers.Remove(target);
        await _db.SaveChangesAsync(ct);
        return (true, "");
    }

    // ==========================================================
    // F9 — TASKS
    // ==========================================================
    public async Task<TaskDto?> CreateTaskAsync(Guid projectId, Guid actorId, TaskRequest req,
                                                CancellationToken ct = default)
    {
        var me = await MembershipAsync(projectId, actorId, ct);
        if (me is null || !ContributorRoles.Contains(me.ProjectRole)) return null;

        // An assignee must already be an active member of this project.
        if (req.AssigneeId is not null)
        {
            var validAssignee = await _db.ProjectMembers.AnyAsync(
                m => m.ProjectId == projectId && m.UserId == req.AssigneeId && m.Status == "Active", ct);
            if (!validAssignee) return null;
        }

        var task = new ProjectTask
        {
            ProjectId = projectId,
            Title = req.Title.Trim(),
            Description = req.Description.Trim(),
            AssigneeId = req.AssigneeId,
            Priority = new[] { "Low", "Medium", "High" }.Contains(req.Priority) ? req.Priority : "Medium",
            DueDate = req.DueDate,
        };

        _db.ProjectTasks.Add(task);
        await _db.SaveChangesAsync(ct);

        // Reload so AssigneeName is populated in the response.
        var saved = await _db.ProjectTasks.Include(t => t.Assignee)
            .FirstAsync(t => t.Id == task.Id, ct);
        return ToTaskDto(saved);
    }

    public async Task<bool> SetTaskStatusAsync(Guid taskId, Guid actorId, string status, CancellationToken ct = default)
    {
        if (!new[] { "Todo", "InProgress", "Done" }.Contains(status)) return false;

        var task = await _db.ProjectTasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
        if (task is null) return false;

        var me = await MembershipAsync(task.ProjectId, actorId, ct);
        if (me is null || !ContributorRoles.Contains(me.ProjectRole)) return false;

        task.Status = status;
        task.CompletedAt = status == "Done" ? DateTime.UtcNow : null;

        if (status == "Done")
        {
            _db.ActivityLogs.Add(new ActivityLog
            {
                UserId = actorId, ActivityType = "Project",
                Description = $"Completed task \"{Format.Truncate(task.Title, 50)}\"",
            });
        }

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteTaskAsync(Guid taskId, Guid actorId, CancellationToken ct = default)
    {
        var task = await _db.ProjectTasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
        if (task is null) return false;

        var me = await MembershipAsync(task.ProjectId, actorId, ct);
        if (me is null || !ManagerRoles.Contains(me.ProjectRole)) return false;

        _db.ProjectTasks.Remove(task);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ==========================================================
    // F8 — MILESTONES
    // ==========================================================
    public async Task<MilestoneDto?> CreateMilestoneAsync(Guid projectId, Guid actorId, MilestoneRequest req,
                                                          CancellationToken ct = default)
    {
        var me = await MembershipAsync(projectId, actorId, ct);
        if (me is null || !ManagerRoles.Contains(me.ProjectRole)) return null;

        var ms = new Milestone { ProjectId = projectId, Title = req.Title.Trim(), DueDate = req.DueDate };
        _db.Milestones.Add(ms);
        await _db.SaveChangesAsync(ct);

        return new MilestoneDto { Id = ms.Id, Title = ms.Title, DueDate = ms.DueDate, IsCompleted = false };
    }

    public async Task<bool> ToggleMilestoneAsync(Guid milestoneId, Guid actorId, CancellationToken ct = default)
    {
        var ms = await _db.Milestones.FirstOrDefaultAsync(m => m.Id == milestoneId, ct);
        if (ms is null) return false;

        var me = await MembershipAsync(ms.ProjectId, actorId, ct);
        if (me is null || !ManagerRoles.Contains(me.ProjectRole)) return false;

        ms.IsCompleted = !ms.IsCompleted;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ==========================================================
    // F10 — FILES
    // The controller writes the bytes; this records the metadata.
    // ==========================================================
    public async Task<ProjectFileDto?> AddFileAsync(Guid projectId, Guid actorId, string fileName,
        string storedName, string contentType, long size, CancellationToken ct = default)
    {
        var me = await MembershipAsync(projectId, actorId, ct);
        if (me is null || !ContributorRoles.Contains(me.ProjectRole)) return null;

        var file = new ProjectFile
        {
            ProjectId = projectId, UploadedById = actorId,
            FileName = fileName, StoredName = storedName,
            ContentType = contentType, SizeBytes = size,
        };

        _db.ProjectFiles.Add(file);
        _db.ActivityLogs.Add(new ActivityLog
        {
            UserId = actorId, ActivityType = "Project",
            Description = $"Uploaded \"{Format.Truncate(fileName, 50)}\"",
        });
        await _db.SaveChangesAsync(ct);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == actorId, ct);
        return new ProjectFileDto
        {
            Id = file.Id, FileName = file.FileName, ContentType = file.ContentType,
            SizeBytes = file.SizeBytes, SizeLabel = Format.FileSize(file.SizeBytes),
            UploadedByName = user?.FullName ?? "Unknown", UploadedAt = file.UploadedAt,
        };
    }

    // Only members may download — checked before any bytes are served.
    public async Task<ProjectFile?> GetFileAsync(Guid fileId, Guid actorId, CancellationToken ct = default)
    {
        var file = await _db.ProjectFiles.FirstOrDefaultAsync(f => f.Id == fileId, ct);
        if (file is null) return null;

        var me = await MembershipAsync(file.ProjectId, actorId, ct);
        return me is null ? null : file;
    }

    public async Task<bool> DeleteFileAsync(Guid fileId, Guid actorId, CancellationToken ct = default)
    {
        var file = await _db.ProjectFiles.FirstOrDefaultAsync(f => f.Id == fileId, ct);
        if (file is null) return false;

        var me = await MembershipAsync(file.ProjectId, actorId, ct);
        // The uploader may remove their own file; managers may remove any.
        if (me is null) return false;
        if (file.UploadedById != actorId && !ManagerRoles.Contains(me.ProjectRole)) return false;

        _db.ProjectFiles.Remove(file);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ---------- HELPERS ----------
    private static TaskDto ToTaskDto(ProjectTask t) => new()
    {
        Id = t.Id, Title = t.Title, Description = t.Description,
        Status = t.Status, Priority = t.Priority,
        AssigneeId = t.AssigneeId, AssigneeName = t.Assignee?.FullName,
        DueDate = t.DueDate,
        IsOverdue = t.DueDate is not null && t.Status != "Done" && t.DueDate < DateTime.UtcNow,
    };

}
