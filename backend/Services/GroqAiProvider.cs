// ============================================================
// MODULE : M8 — AI Intelligence
// LAYER  : Service — fallback AI provider
// PURPOSE: Text generation via Groq's OpenAI-compatible endpoint.
//          Used only when Gemini fails (see ResilientAiProvider).
// SETUP  : Put a free key in backend/appsettings.Development.json:
//            "Groq": { "ApiKey": "gsk_..." }
//          Get one at console.groq.com/keys — free, no card.
//          NOTE: model availability varies per account. Check yours with
//            curl -H "Authorization: Bearer $KEY" \
//                 https://api.groq.com/openai/v1/models
//          Llama models were NOT available on this project's key; the
//          most capable one that is, and which returns clean JSON, is
//          openai/gpt-oss-120b.
//          With no key configured this provider simply returns null,
//          so the app behaves exactly as it did before.
// ============================================================
using System.Text;
using System.Text.Json;

namespace AiInnovationHub.Api.Services;

public class GroqAiProvider : IAiProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<GroqAiProvider> _log;
    private readonly string _apiKey;
    private readonly string _model;

    private const string Endpoint = "https://api.groq.com/openai/v1/chat/completions";

    public string LastProviderUsed { get; private set; } = "unavailable";

    public GroqAiProvider(HttpClient http, IConfiguration config, ILogger<GroqAiProvider> log)
    {
        _http = http; _log = log;
        _apiKey = config["Groq:ApiKey"] ?? "";
        _model  = config["Groq:Model"] ?? "openai/gpt-oss-120b";
    }

    public async Task<string?> GenerateTextAsync(string prompt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _log.LogInformation("No Groq key configured — fallback unavailable.");
            return null;
        }

        try
        {
            // Groq mirrors the OpenAI chat-completions shape.
            var payload = new
            {
                model = _model,
                messages = new[] { new { role = "user", content = prompt } },
                temperature = 0.7,
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            req.Headers.Add("Authorization", $"Bearer {_apiKey}");
            req.Content = new StringContent(
                JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var res = await _http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode)
            {
                _log.LogWarning("Groq call failed: {Status}", res.StatusCode);
                return null;
            }
            LastProviderUsed = "groq";

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
            return doc.RootElement.GetProperty("choices")[0]
                      .GetProperty("message").GetProperty("content").GetString();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Groq text generation threw.");
            return null;
        }
    }

    // Groq exposes no embedding endpoint — F3 stays on Gemini embeddings.
    public Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
        => Task.FromResult<float[]?>(null);
}
