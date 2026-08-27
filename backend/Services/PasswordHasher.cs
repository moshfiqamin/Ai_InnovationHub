// ============================================================
// MODULE : M1 — Authentication
// LAYER  : Service (business logic supporting the Controller)
// PURPOSE: Hash and verify passwords using PBKDF2.
// WHY HAND-WRITTEN: The CSE470 guide penalises third-party
//   libraries that implement a major feature. PBKDF2 here uses only
//   .NET's built-in System.Security.Cryptography — no external
//   auth/hashing package is involved.
// SECURITY: Random 128-bit salt per user, 100,000 iterations,
//   SHA-256, and a constant-time comparison to resist timing attacks.
// ============================================================
using System.Security.Cryptography;

namespace AiInnovationHub.Api.Services;

public static class PasswordHasher
{
    private const int SaltSize = 16;        // 128-bit salt
    private const int KeySize = 32;         // 256-bit derived key
    private const int Iterations = 100_000; // deliberate slowdown vs brute force

    // ---- CREATE ----
    // Returns a fresh salt and the hash derived from password + salt.
    public static (string hash, string salt) HashPassword(string password)
    {
        byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            password, saltBytes, Iterations, HashAlgorithmName.SHA256, KeySize);

        return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
    }

    // ---- VERIFY ----
    // Re-derives the hash from the supplied password using the stored
    // salt, then compares. Returns false on any malformed stored value.
    public static bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        try
        {
            byte[] saltBytes = Convert.FromBase64String(storedSalt);
            byte[] expected = Convert.FromBase64String(storedHash);

            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
                password, saltBytes, Iterations, HashAlgorithmName.SHA256, KeySize);

            // Constant-time comparison: never leaks how much of the hash matched.
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false;
        }
    }
}
