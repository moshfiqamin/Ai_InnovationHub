// ============================================================
// MODULE : M6 — Project Collaboration
// LAYER  : Model (MVC: M)
// FEATURE: F7 — Team Formation ("invite/join team", "assign team roles")
// PURPOSE: Links a user to a project with a project-scoped role. This
//          role is separate from the platform-wide role on User: a
//          platform Investor can still be a project Contributor.
// ============================================================
namespace AiInnovationHub.Api.Models.Entities;

public class ProjectMember
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    // Owner | Maintainer | Contributor | Viewer
    public string ProjectRole { get; set; } = "Contributor";

    // Invited -> Active once the invitee accepts (F7 invite/join flow)
    public string Status { get; set; } = "Invited";

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
