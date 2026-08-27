// ============================================================
// MODULE : M8 — AI Intelligence
// LAYER  : Service — concrete AI provider
// PURPOSE: Talks to Google's Gemini REST API.
// MODELS : gemini-3.6-flash (text), gemini-embedding-001 (vectors)
// NOTE   : gemini-2.5-flash is retired for new API keys, which is why
//          3.6-flash is used. Embeddings are requested at 1536
//          dimensions because pgvector indexes cap at 2000 (the model
//          returns 3072 by default).
// ============================================================
using System.Text;
using System.Text.Json;

namespace AiInnovationHub.Api.Services;

public class GeminiAiProvider : IAiProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<GeminiAiProvider> _log;
    private readonly string _apiKey;
    private readonly string _textModel;
    private readonly string _embedModel;
    private readonly int _embedDims;

    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    public string LastProviderUsed { get; private set; } = "unavailable";

    public GeminiAiProvider(HttpClient http, IConfiguration config, ILogger<GeminiAiProvider> log)
    {
        _http = http;
        _log = log;
        // Values come from appsettings.json / environment variables.
        _apiKey     = config["Gemini:ApiKey"] ?? "";
        _textModel  = config["Gemini:TextModel"]  ?? "gemini-3.6-flash";
        _embedModel = config["Gemini:EmbedModel"] ?? "gemini-embedding-001";
        _embedDims  = int.TryParse(config["Gemini:EmbedDims"], out var d) ? d : 1536;
    }

    // ---- TEXT GENERATION -------------------------------------
    public async Task<string?> GenerateTextAsync(string prompt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _log.LogWarning("Gemini API key missing — skipping AI call.");
            return null;
        }

        try
        {
            // Gemini expects: { "contents": [ { "parts": [ { "text": ... } ] } ] }
            var payload = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } }
            };

            using var req = new HttpRequestMessage(
                HttpMethod.Post, $"{BaseUrl}/{_textModel}:generateContent");
            req.Headers.Add("x-goog-api-key", _apiKey);
            req.Content = new StringContent(
                JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var res = await _http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode)
            {
                // Log and return null — the caller falls back (NFR10).
                _log.LogWarning("Gemini text call failed: {Status}", res.StatusCode);
                return null;
            }
            LastProviderUsed = "gemini";

            // Walk the response: candidates[0].content.parts[0].text
            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
            return doc.RootElement
                      .GetProperty("candidates")[0]
                      .GetProperty("content")
                      .GetProperty("parts")[0]
                      .GetProperty("text")
                      .GetString();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Gemini text generation threw an exception.");
            return null;
        }
    }

    // ---- EMBEDDINGS (for F3 / F6 in a later sprint) ----------
    public async Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) return null;

        try
        {
            var payload = new
            {
                model = $"models/{_embedModel}",
                content = new { parts = new[] { new { text } } },
                outputDimensionality = _embedDims,   // keep vectors pgvector-indexable
            };

            using var req = new HttpRequestMessage(
                HttpMethod.Post, $"{BaseUrl}/{_embedModel}:embedContent");
            req.Headers.Add("x-goog-api-key", _apiKey);
            req.Content = new StringContent(
                JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var res = await _http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode)
            {
                _log.LogWarning("Gemini embedding call failed: {Status}", res.StatusCode);
                return null;
            }

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
            return doc.RootElement
                      .GetProperty("embedding")
                      .GetProperty("values")
                      .EnumerateArray()
                      .Select(v => v.GetSingle())
                      .ToArray();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Gemini embedding threw an exception.");
            return null;
        }
    }
}
