// ============================================================
// MODULE : M1 — Authentication
// LAYER  : Model (MVC: M) — Data Transfer Objects
// PURPOSE: Defines exactly what crosses the API boundary. Using
//          DTOs instead of entities stops password hashes ever
//          being serialised to the client (NFR4 Security).
// ============================================================
using System.ComponentModel.DataAnnotations;

namespace AiInnovationHub.Api.Models.DTOs;

// ---- INCOMING: registration form (M1) ----
// [Required]/[EmailAddress] give server-side validation (NFR5),
// independent of the checks the React form already performs.
public class RegisterRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    // Explicit ErrorMessage: the framework default leaks the C# property
    // name ("The field FullName must be...") which NFR6 asks us to avoid.
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "Full name must be between 2 and 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = "Innovator";
}

// ---- INCOMING: login form (M1) ----
public class LoginRequest
{
    [Required] [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

// ---- OUTGOING: safe view of a user (no credentials) ----
public class UserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int ReputationPoints { get; set; }
}

// ---- OUTGOING: successful login/registration ----
public class AuthResponse
{
    public string Token { get; set; } = string.Empty;  // JWT
    public UserDto User { get; set; } = new();
}

// ---- OUTGOING: consistent error shape (NFR6) ----
// The React layer reads err.response.data.message.
public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public ErrorResponse(string message) => Message = message;
}
