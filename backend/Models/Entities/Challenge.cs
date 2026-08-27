// ============================================================
// MODULE : M9 — Innovation Challenges
// LAYER  : Model (MVC: M)
// FEATURE: F14 — Innovation Challenges
// ============================================================
namespace AiInnovationHub.Api.Models.Entities;

public class Challenge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Prize { get; set; } = string.Empty;

    public DateTime Deadline { get; set; }
    // Open | Judging | Closed — derived from the deadline but stored so
    // an organiser can close a challenge early.
    public string Status { get; set; } = "Open";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Only Organization / Admin roles may create challenges (M9).
    public Guid CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public ICollection<ChallengeSubmission> Submissions { get; set; } = new List<ChallengeSubmission>();
}

public class ChallengeSubmission
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Submitted | Scored
    public string Status { get; set; } = "Submitted";
    public int? Score { get; set; }            // 0-100, set by the organiser
    public string? Feedback { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public Guid ChallengeId { get; set; }
    public Challenge? Challenge { get; set; }

    // A submission always points at one of the entrant's ideas.
    public Guid IdeaId { get; set; }
    public Idea? Idea { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }
}
