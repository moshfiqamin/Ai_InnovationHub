// ============================================================
// MODULE : M1 — Authentication
// LAYER  : Controller (MVC: C)
// PURPOSE: HTTP endpoints for registration, login and identity.
//          Handles requests/responses only — hashing lives in
//          PasswordHasher, token creation in TokenService.
// ROUTES :
//   POST /api/auth/register  create an account and sign in
//   POST /api/auth/login     sign in to an existing account
//   GET  /api/auth/me        return the current user (JWT required)
// NOTE   : The CSE470 guide excludes login/registration from the
//          20 counted features — this is supporting infrastructure.
// ============================================================
using System.Security.Claims;
using AiInnovationHub.Api.Data;
using AiInnovationHub.Api.Models.DTOs;
using AiInnovationHub.Api.Models.Entities;
using AiInnovationHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiInnovationHub.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : BaseApiController
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokens;

    // ---- ROLE ALLOW-LIST ----
    // The only roles a visitor may assign to themselves during registration.
    // Privileged roles (Judge, Moderator, Admin) are deliberately absent:
    // they are granted through the Administration module (M14).
    private static readonly HashSet<string> SelfServiceRoles = new(StringComparer.Ordinal)
    {
        "Innovator", "Researcher", "Entrepreneur", "Mentor", "Investor", "Organization",
    };

    // Dependencies arrive through the constructor (DI, configured in Program.cs)
    public AuthController(AppDbContext db, ITokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    // ==========================================================
    // POST /api/auth/register
    // Creates an account, logs the first activity, returns a JWT.
    // ==========================================================
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        // ---- 1. VALIDATE (NFR5) ----
        // [ApiController] auto-runs the DTO's data annotations, but we
        // check explicitly so the error shape stays consistent (NFR6).
        if (!ModelState.IsValid)
            return Fail("Please check the form and try again.");

        var email = req.Email.Trim().ToLowerInvariant();

        // ---- 2. REJECT DUPLICATE EMAILS ----
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            return Conflict(new ErrorResponse("An account with this email already exists."));

        // ---- 3. RESTRICT ROLE VALUES ----
        // Never trust a role sent from the browser. An attacker could post
        // "Admin" straight to this endpoint, so only the self-service roles
        // are accepted here. Judge / Moderator / Admin are privileged and
        // must be granted by an administrator in M14, never chosen at
        // sign-up. Anything unrecognised silently falls back to Innovator.
        // NOTE: keep in sync with frontend/src/constants/roles.js
        var role = SelfServiceRoles.Contains(req.Role) ? req.Role : "Innovator";

        // ---- 4. HASH THE PASSWORD (never store plain text) ----
        var (hash, salt) = PasswordHasher.HashPassword(req.Password);

        var user = new User
        {
            FullName     = req.FullName.Trim(),
            Email        = email,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role         = role,
        };

        _db.Users.Add(user);

        // ---- 5. RECORD THE SIGN-UP (feeds M3's activity panel) ----
        _db.ActivityLogs.Add(new ActivityLog
        {
            UserId = user.Id,
            ActivityType = "Auth",
            Description = "Joined AI_InnovationHub",
        });

        await _db.SaveChangesAsync(ct);

        // ---- 6. ISSUE A TOKEN so the user is signed in immediately ----
        return Ok(new AuthResponse { Token = _tokens.CreateToken(user), User = ToDto(user) });
    }

    // ==========================================================
    // POST /api/auth/login
    // Verifies credentials and returns a JWT.
    // ==========================================================
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return Fail("Please enter a valid email and password.");

        var email = req.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // ---- SECURITY: identical message for "no such user" and
        // "wrong password". Revealing which one is wrong would let an
        // attacker discover which emails are registered.
        if (user is null || !PasswordHasher.VerifyPassword(req.Password, user.PasswordHash, user.PasswordSalt))
            return Unauthorized(new ErrorResponse("Invalid email or password."));

        return Ok(new AuthResponse { Token = _tokens.CreateToken(user), User = ToDto(user) });
    }

    // ==========================================================
    // GET /api/auth/me
    // Returns the signed-in user. [Authorize] rejects requests
    // without a valid JWT before this method ever runs.
    // ==========================================================
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == UserId, ct);
        if (user is null) return Missing("Account no longer exists.");

        return Ok(ToDto(user));
    }


    // ---- HELPER: entity -> safe DTO (strips password fields) ----
    private static UserDto ToDto(User u) => new()
    {
        Id = u.Id, FullName = u.FullName, Email = u.Email,
        Role = u.Role, ReputationPoints = u.ReputationPoints,
    };
}
