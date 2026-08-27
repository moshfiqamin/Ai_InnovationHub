// ============================================================
// MODULE : M8 — AI Intelligence
// LAYER  : Service — provider composition
// PURPOSE: Try Gemini first; if it is rate-limited or unreachable,
//          transparently retry through Groq.
// WHY    : Decision D9 in PROJECT_REFERENCE anticipated Gemini's free
//          tier running out. It did (HTTP 429 RESOURCE_EXHAUSTED during
//          M4-M6 testing), so the fallback is now wired up for real.
//          Callers still depend on IAiProvider and know nothing about
//          which backend answered (NFR11 Scalability).
// ============================================================
namespace AiInnovationHub.Api.Services;

public class ResilientAiProvider : IAiProvider
{
    private readonly GeminiAiProvider _primary;
    private readonly GroqAiProvider _fallback;
    private readonly ILogger<ResilientAiProvider> _log;

    // Reports the provider that actually produced the last answer.
    public string LastProviderUsed { get; private set; } = "unavailable";

    public ResilientAiProvider(GeminiAiProvider primary, GroqAiProvider fallback,
                               ILogger<ResilientAiProvider> log)
    {
        _primary = primary; _fallback = fallback; _log = log;
    }

    public async Task<string?> GenerateTextAsync(string prompt, CancellationToken ct = default)
    {
        var result = await _primary.GenerateTextAsync(prompt, ct);
        if (result is not null)
        {
            LastProviderUsed = "gemini";
            return result;
        }

        _log.LogWarning("Gemini text generation failed — falling back to Groq.");
        var viaGroq = await _fallback.GenerateTextAsync(prompt, ct);
        LastProviderUsed = viaGroq is not null ? "groq" : "unavailable";
        if (viaGroq is not null) _log.LogInformation("Groq answered successfully.");
        return viaGroq;
    }

    public async Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        // Groq serves no embedding model, so embeddings stay Gemini-only.
        // F3 therefore degrades to "no similar ideas" rather than wrong ones,
        // which is the safer failure for a duplicate-detection feature.
        return await _primary.GenerateEmbeddingAsync(text, ct);
    }
}
