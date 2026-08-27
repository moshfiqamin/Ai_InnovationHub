// ============================================================
// MODULE : M14 — Administration
// LAYER  : Model (MVC: M)
// FEATURE: F20 — Admin & AI Content Moderation
// PURPOSE: A piece of content flagged for review, either by a user
//          report or by the AI moderation pass.
// NFR    : NFR15 Auditability
// ============================================================
namespace AiInnovationHub.Api.Models.Entities;

public class ContentReport
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Idea | Post | Comment
    public string TargetType { get; set; } = "Idea";
    public Guid TargetId { get; set; }
    public string TargetPreview { get; set; } = string.Empty;  // snapshot of the text

    public string Reason { get; set; } = string.Empty;

    // Pending | Dismissed | ActionTaken
    public string Status { get; set; } = "Pending";

    // ---- AI MODERATION (F20) ----
    // Null until the AI pass runs. Verdict is Safe | Review | Unsafe.
    public string? AiVerdict { get; set; }
    public string? AiReason { get; set; }
    public DateTime? AiCheckedAt { get; set; }

    // Null when the AI raised it rather than a person.
    public Guid? ReporterId { get; set; }
    public User? Reporter { get; set; }

    public Guid? ResolvedById { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
