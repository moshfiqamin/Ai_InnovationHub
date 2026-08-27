// ============================================================
// MODULE : M1 — Authentication
// LAYER  : Service (business logic supporting the Controller)
// PURPOSE: Issues JSON Web Tokens after a successful login or
//          registration. The token proves identity on later requests
//          without the server storing any session (NFR14).
// ============================================================
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AiInnovationHub.Api.Models.Entities;
using Microsoft.IdentityModel.Tokens;

namespace AiInnovationHub.Api.Services;

// Interface first so the controller depends on an abstraction,
// not a concrete class (dependency inversion, NFR8 Maintainability).
public interface ITokenService
{
    string CreateToken(User user);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    public TokenService(IConfiguration config) => _config = config;

    public string CreateToken(User user)
    {
        // ---- 1. CLAIMS: the facts embedded in the token ----
        // The API reads these instead of hitting the database on every request.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role),          // drives role-based access
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        // ---- 2. SIGNING KEY ----
        // Read from configuration/environment, never hardcoded (NFR4).
        var secret = _config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // ---- 3. BUILD THE TOKEN ----
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),   // session lifetime
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
