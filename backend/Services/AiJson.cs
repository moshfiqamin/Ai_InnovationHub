// ============================================================
// FILE   : Services/AiJson.cs
// LAYER  : Service — shared AI response parsing
// PURPOSE: Language models often wrap JSON in markdown fences or add a
//          sentence before it. The same "slice from the first brace to
//          the last, then deserialise, then swallow failures" block was
//          copy-pasted into five services. It lives here once.
// ============================================================
using System.Text.Json;

namespace AiInnovationHub.Api.Services;

public static class AiJson
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    // Extracts and deserialises a JSON OBJECT from a model reply.
    public static T? Object<T>(string? raw) where T : class => Extract<T>(raw, '{', '}');

    // Extracts and deserialises a JSON ARRAY from a model reply.
    public static T? Array<T>(string? raw) where T : class => Extract<T>(raw, '[', ']');

    // Returns the raw JSON substring, for callers that want to cache it.
    public static string? Slice(string? raw, char open = '{', char close = '}')
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var start = raw.IndexOf(open);
        var end = raw.LastIndexOf(close);
        return start < 0 || end <= start ? null : raw[start..(end + 1)];
    }

    private static T? Extract<T>(string? raw, char open, char close) where T : class
    {
        var json = Slice(raw, open, close);
        if (json is null) return null;
        // A malformed reply must never crash a request — the caller
        // decides what to show when this returns null (NFR10).
        try { return JsonSerializer.Deserialize<T>(json, Options); }
        catch { return null; }
    }
}
