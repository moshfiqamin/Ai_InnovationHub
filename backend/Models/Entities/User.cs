// ============================================================
// MODULE : M1 — Authentication
// LAYER  : Model (MVC: M) — database entity
// PURPOSE: Represents a registered account. Mapped to the "Users"
//          table by Entity Framework Core.
// SECURITY: The raw password is NEVER stored — only a PBKDF2 hash
//           and the per-user random salt used to produce it (NFR4).
// ============================================================
namespace AiInnovationHub.Api.Models.Entities;

public class User
{
    // Primary key. Guid instead of int so IDs are not guessable.
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FullName { get; set; } = string.Empty;

    // Unique — enforced by an index in AppDbContext.
    public string Email { get; set; } = string.Empty;

    // ---- CREDENTIALS (never expose these in any DTO) ----
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;

    // Role drives authorization: Innovator | Mentor | Investor | Admin
    public string Role { get; set; } = "Innovator";

    // ---- PROFILE FIELDS used by M3's AI recommendations (F18) ----
    public string Skills { get; set; } = string.Empty;     // comma separated
    public string Interests { get; set; } = string.Empty;  // comma separated

    // ---- REPUTATION (M13 / F16) ----
    public int ReputationPoints { get; set; } = 0;

    // ---- M13 PROFILE FIELDS ----
    public string Bio { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;   // e.g. "IoT engineer"
    public string Location { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;

    // ---- M10: mentors and investors expose extra detail ----
    public string Expertise { get; set; } = string.Empty;      // mentors
    public string InvestmentFocus { get; set; } = string.Empty; // investors
    public bool IsAvailableForMentoring { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ---- NAVIGATION PROPERTIES (EF Core relationships) ----
    public ICollection<Idea> Ideas { get; set; } = new List<Idea>();
    public ICollection<ActivityLog> Activities { get; set; } = new List<ActivityLog>();
}
