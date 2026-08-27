// ============================================================
// MODULE : M6 — Project Collaboration
// LAYER  : Model (MVC: M)
// FEATURE: F8 — Project Workspace ("milestones/deadlines")
// ============================================================
namespace AiInnovationHub.Api.Models.Entities;

public class Milestone
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public bool IsCompleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
}
