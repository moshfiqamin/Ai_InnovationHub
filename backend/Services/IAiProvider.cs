// ============================================================
// MODULE : M8 — AI Intelligence (service layer used by M3 now)
// LAYER  : Service
// PURPOSE: Provider-agnostic contract for all AI calls.
// WHY AN INTERFACE: Gemini's free tier is rate limited. Swapping to
//   Groq or another provider means writing one new class and changing
//   one DI registration in Program.cs — no controller or page changes
//   (NFR11 Scalability).
// ============================================================
namespace AiInnovationHub.Api.Services;

public interface IAiProvider
{
    // Which provider answered the most recent call in THIS request:
    // "gemini", "groq", or "unavailable". Services are scoped per request,
    // so this cannot bleed between concurrent users. Exposed so the UI can
    // label results honestly instead of assuming Gemini.
    string LastProviderUsed { get; }

    // Sends a prompt and returns the model's raw text answer.
    // Returns null when the provider is unavailable so callers can
    // degrade gracefully rather than crash (NFR10 Reliability).
    Task<string?> GenerateTextAsync(string prompt, CancellationToken ct = default);

    // Converts text to a vector for pgvector similarity search.
    // Used later by F3 (Similar Idea Detection) and F6 (Smart Search).
    Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken ct = default);
}
