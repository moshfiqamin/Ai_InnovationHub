// ============================================================
// MODULE : M6 — Project Collaboration
// LAYER  : Model (MVC: M)
// FEATURE: F9 — Task Management
// NOTE   : Named ProjectTask rather than Task to avoid colliding with
//          System.Threading.Tasks.Task.
// ============================================================
namespace AiInnovationHub.Api.Models.Entities;

public class ProjectTask
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Todo | InProgress | Done
    public string Status { get; set; } = "Todo";

    // Low | Medium | High
    public string Priority { get; set; } = "Medium";

    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    // Null while the task sits unassigned in the backlog.
    public Guid? AssigneeId { get; set; }
    public User? Assignee { get; set; }
}
