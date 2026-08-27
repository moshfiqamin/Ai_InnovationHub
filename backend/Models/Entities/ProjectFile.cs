// ============================================================
// MODULE : M6 — Project Collaboration
// LAYER  : Model (MVC: M)
// FEATURE: F10 — File & Resource Sharing
// SECURITY: Only metadata lives in the database. Bytes are written to
//   backend/uploads/ under a generated GUID name, never the name the
//   browser supplied — that prevents path traversal via crafted
//   filenames such as "../../appsettings.json" (NFR4).
// ============================================================
namespace AiInnovationHub.Api.Models.Entities;

public class ProjectFile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // The name shown in the UI (as uploaded, sanitised for display)
    public string FileName { get; set; } = string.Empty;

    // The generated name actually on disk
    public string StoredName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid UploadedById { get; set; }
    public User? UploadedBy { get; set; }
}
