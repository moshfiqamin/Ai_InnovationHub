// ============================================================
// MODULE : M6 — Project Collaboration
// LAYER  : Model (MVC: M)
// FEATURE: F8 — Project Workspace
// PURPOSE: A project, optionally promoted from an existing idea
//          ("Create project from idea" in requirements.pdf M6).
// ============================================================
namespace AiInnovationHub.Api.Models.Entities;

public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Planning | Active | Completed | OnHold
    public string Status { get; set; } = "Planning";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ---- OWNER (creator, always also a member with role Owner) ----
    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }

    // ---- SOURCE IDEA (optional) ----
    // Null when the project was created from scratch rather than promoted.
    public Guid? SourceIdeaId { get; set; }
    public Idea? SourceIdea { get; set; }

    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
    public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
    public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
    public ICollection<ProjectFile> Files { get; set; } = new List<ProjectFile>();
}
