// ============================================================
// MODULE : M6 — Project Collaboration
// LAYER  : Model (MVC: M) — Data Transfer Objects
// FEATURES: F7 Team Formation · F8 Project Workspace
//           F9 Task Management · F10 File & Resource Sharing
// ============================================================
using System.ComponentModel.DataAnnotations;

namespace AiInnovationHub.Api.Models.DTOs;

// ---- F8: create a project (optionally from an idea) ----
public class ProjectRequest
{
    [Required(ErrorMessage = "A project title is required.")]
    [StringLength(200, MinimumLength = 3,
        ErrorMessage = "Title must be between 3 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    // When set, the project records which idea it grew out of.
    public Guid? SourceIdeaId { get; set; }
}

// ---- F8: project summary card ----
public class ProjectCardDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public int TaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
    public string MyRole { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ---- F7: a team member ----
public class MemberDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ProjectRole { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

// ---- F7: invite someone by email ----
public class InviteRequest
{
    [Required(ErrorMessage = "An email address is required.")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
    public string Email { get; set; } = string.Empty;

    // Maintainer | Contributor | Viewer  (never Owner)
    public string ProjectRole { get; set; } = "Contributor";
}

// ---- F7: change an existing member's project role ----
public class MemberRoleRequest
{
    [Required] public string ProjectRole { get; set; } = "Contributor";
}

// ---- F9: create or update a task ----
public class TaskRequest
{
    [Required(ErrorMessage = "A task title is required.")]
    [StringLength(200, MinimumLength = 2,
        ErrorMessage = "Task title must be between 2 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    public Guid? AssigneeId { get; set; }
    public string Priority { get; set; } = "Medium";
    public DateTime? DueDate { get; set; }
}

public class TaskStatusRequest
{
    [Required] public string Status { get; set; } = "Todo";  // Todo|InProgress|Done
}

public class TaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public Guid? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsOverdue { get; set; }
}

// ---- F8: milestones ----
public class MilestoneRequest
{
    [Required(ErrorMessage = "A milestone title is required.")]
    [StringLength(200, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
}

public class MilestoneDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public bool IsCompleted { get; set; }
}

// ---- F10: an uploaded resource ----
public class ProjectFileDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string SizeLabel { get; set; } = string.Empty;  // e.g. "1.4 MB"
    public string UploadedByName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}

// ---- F8: the whole workspace payload ----
public class ProjectWorkspaceDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public Guid? SourceIdeaId { get; set; }
    public string? SourceIdeaTitle { get; set; }
    public string MyRole { get; set; } = string.Empty;
    public bool CanManage { get; set; }   // Owner or Maintainer

    public List<MemberDto> Members { get; set; } = new();
    public List<TaskDto> Tasks { get; set; } = new();
    public List<MilestoneDto> Milestones { get; set; } = new();
    public List<ProjectFileDto> Files { get; set; } = new();
}
