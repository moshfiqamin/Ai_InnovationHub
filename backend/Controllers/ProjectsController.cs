// ============================================================
// MODULE : M6 — Project Collaboration
// LAYER  : Controller (MVC: C)
// FEATURES: F7 Team · F8 Workspace · F9 Tasks · F10 Files
// ROUTES :
//   GET    /api/projects                          my projects        F8
//   POST   /api/projects                          create             F8
//   GET    /api/projects/{id}                     workspace          F8
//   POST   /api/projects/{id}/invite              invite a member    F7
//   POST   /api/projects/{id}/accept              accept invite      F7
//   PUT    /api/projects/{id}/members/{userId}    change role        F7
//   DELETE /api/projects/{id}/members/{userId}    remove member      F7
//   POST   /api/projects/{id}/tasks               create task        F9
//   PUT    /api/projects/tasks/{taskId}/status    move task          F9
//   DELETE /api/projects/tasks/{taskId}           delete task        F9
//   POST   /api/projects/{id}/milestones          add milestone      F8
//   PUT    /api/projects/milestones/{msId}/toggle complete/undo      F8
//   POST   /api/projects/{id}/files               upload             F10
//   GET    /api/projects/files/{fileId}           download           F10
//   DELETE /api/projects/files/{fileId}           delete             F10
// ============================================================
using AiInnovationHub.Api.Models.DTOs;
using AiInnovationHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiInnovationHub.Api.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController : BaseApiController
{
    private readonly IProjectService _projects;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ProjectsController> _log;

    // ---- F10 UPLOAD LIMITS ----
    private const long MaxUploadBytes = 10 * 1024 * 1024;   // 10 MB
    // Allow-list, not a block-list: anything not named here is refused,
    // so a new dangerous extension cannot slip through (NFR4).
    private static readonly string[] AllowedExtensions =
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg",
        ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".txt", ".md", ".csv", ".json", ".zip",
    };

    public ProjectsController(IProjectService projects, IWebHostEnvironment env,
                              ILogger<ProjectsController> log)
    {
        _projects = projects; _env = env; _log = log;
    }

    // ================= F8 : PROJECTS =================
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        return Ok(await _projects.GetMyProjectsAsync(UserId, ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProjectRequest req, CancellationToken ct)
    {
        var id = await _projects.CreateAsync(UserId, req, ct);
        return id is null
            ? Fail("You can only create a project from one of your own ideas.")
            : Ok(new { id });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetWorkspace(Guid id, CancellationToken ct)
    {
        var ws = await _projects.GetWorkspaceAsync(id, UserId, ct);
        // 404 rather than 403 so non-members cannot even confirm it exists.
        return ws is null
            ? Missing("Project not found, or you are not a member.")
            : Ok(ws);
    }

    // ================= F7 : TEAM =================
    [HttpPost("{id:guid}/invite")]
    public async Task<IActionResult> Invite(Guid id, [FromBody] InviteRequest req, CancellationToken ct)
    {
        var (ok, error) = await _projects.InviteAsync(id, UserId, req, ct);
        return ok ? Ok(new { invited = true }) : Fail(error);
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct)
    {
        return await _projects.AcceptInviteAsync(id, UserId, ct)
            ? Ok(new { joined = true })
            : Missing("No pending invitation found for this project.");
    }

    [HttpPut("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> ChangeRole(Guid id, Guid userId,
        [FromBody] MemberRoleRequest req, CancellationToken ct)
    {
        var (ok, error) = await _projects.ChangeMemberRoleAsync(id, UserId, userId, req.ProjectRole, ct);
        return ok ? Ok(new { updated = true }) : Fail(error);
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken ct)
    {
        var (ok, error) = await _projects.RemoveMemberAsync(id, UserId, userId, ct);
        return ok ? Ok(new { removed = true }) : Fail(error);
    }

    // ================= F9 : TASKS =================
    [HttpPost("{id:guid}/tasks")]
    public async Task<IActionResult> CreateTask(Guid id, [FromBody] TaskRequest req, CancellationToken ct)
    {
        var task = await _projects.CreateTaskAsync(id, UserId, req, ct);
        return task is null
            ? Fail("Could not create that task. Check your permissions and the assignee.")
            : Ok(task);
    }

    [HttpPut("tasks/{taskId:guid}/status")]
    public async Task<IActionResult> SetTaskStatus(Guid taskId, [FromBody] TaskStatusRequest req, CancellationToken ct)
    {
        return await _projects.SetTaskStatusAsync(taskId, UserId, req.Status, ct)
            ? Ok(new { status = req.Status })
            : Fail("Could not update that task.");
    }

    [HttpDelete("tasks/{taskId:guid}")]
    public async Task<IActionResult> DeleteTask(Guid taskId, CancellationToken ct)
    {
        return await _projects.DeleteTaskAsync(taskId, UserId, ct)
            ? Ok(new { deleted = true })
            : Fail("Could not delete that task.");
    }

    // ================= F8 : MILESTONES =================
    [HttpPost("{id:guid}/milestones")]
    public async Task<IActionResult> CreateMilestone(Guid id, [FromBody] MilestoneRequest req, CancellationToken ct)
    {
        var ms = await _projects.CreateMilestoneAsync(id, UserId, req, ct);
        return ms is null
            ? Fail("Only the owner or a maintainer can add milestones.")
            : Ok(ms);
    }

    [HttpPut("milestones/{msId:guid}/toggle")]
    public async Task<IActionResult> ToggleMilestone(Guid msId, CancellationToken ct)
    {
        return await _projects.ToggleMilestoneAsync(msId, UserId, ct)
            ? Ok(new { toggled = true })
            : Fail("Could not update that milestone.");
    }

    // ================= F10 : FILES =================
    [HttpPost("{id:guid}/files")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> UploadFile(Guid id, IFormFile? file, CancellationToken ct)
    {
        // ---- 1. VALIDATE THE UPLOAD ----
        if (file is null || file.Length == 0)
            return Fail("Please choose a file to upload.");
        if (file.Length > MaxUploadBytes)
            return Fail("Files must be 10 MB or smaller.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return Fail($"'{ext}' files are not allowed.");

        // ---- 2. WRITE THE BYTES ----
        // The stored name is a fresh GUID, so a crafted filename such as
        // "../../appsettings.json" cannot escape the uploads folder.
        var uploadDir = Path.Combine(_env.ContentRootPath, "uploads");
        Directory.CreateDirectory(uploadDir);

        var storedName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadDir, storedName);

        try
        {
            await using var stream = System.IO.File.Create(fullPath);
            await file.CopyToAsync(stream, ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed writing upload for project {ProjectId}", id);
            return StatusCode(500, new ErrorResponse("The file could not be saved."));
        }

        // ---- 3. RECORD THE METADATA ----
        var dto = await _projects.AddFileAsync(id, UserId,
            Path.GetFileName(file.FileName), storedName, file.ContentType ?? "application/octet-stream",
            file.Length, ct);

        if (dto is null)
        {
            // Permission denied after the write — remove the orphan file.
            System.IO.File.Delete(fullPath);
            return Fail("You do not have permission to upload to this project.");
        }

        return Ok(dto);
    }

    [HttpGet("files/{fileId:guid}")]
    public async Task<IActionResult> DownloadFile(Guid fileId, CancellationToken ct)
    {
        var file = await _projects.GetFileAsync(fileId, UserId, ct);
        if (file is null) return Missing("File not found, or you are not a member.");

        var path = Path.Combine(_env.ContentRootPath, "uploads", file.StoredName);
        if (!System.IO.File.Exists(path))
            return Missing("The stored file is missing from disk.");

        return PhysicalFile(path, file.ContentType, file.FileName);
    }

    [HttpDelete("files/{fileId:guid}")]
    public async Task<IActionResult> DeleteFile(Guid fileId, CancellationToken ct)
    {
        var file = await _projects.GetFileAsync(fileId, UserId, ct);
        if (file is null) return Missing("File not found.");

        var storedName = file.StoredName;
        if (!await _projects.DeleteFileAsync(fileId, UserId, ct))
            return Fail("You do not have permission to delete that file.");

        // Remove the bytes only after the row is gone.
        var path = Path.Combine(_env.ContentRootPath, "uploads", storedName);
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);

        return Ok(new { deleted = true });
    }
}
