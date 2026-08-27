// ============================================================
// FILE   : Services/Format.cs
// LAYER  : Service — shared formatting helpers
// PURPOSE: One home for the small display helpers that were previously
//          defined on IdeaService and ProjectService and called across
//          eight other services. Putting them here removes the odd
//          cross-service dependency and gives one place to change how
//          dates and sizes read.
// ============================================================
namespace AiInnovationHub.Api.Services;

public static class Format
{
    // "just now" / "3 hours ago" / "1 day ago" / "4 Mar 2026"
    public static string TimeAgo(DateTime utc)
    {
        var span = DateTime.UtcNow - utc;
        if (span.TotalMinutes < 1)  return "just now";
        if (span.TotalMinutes < 60) return Plural((int)span.TotalMinutes, "minute");
        if (span.TotalHours   < 24) return Plural((int)span.TotalHours, "hour");
        if (span.TotalDays    < 30) return Plural((int)span.TotalDays, "day");
        return utc.ToString("d MMM yyyy");
    }

    // Cuts long text for previews and log lines, adding an ellipsis.
    public static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max].TrimEnd() + "…";

    // "ai, recycling, campus" -> ["ai", "recycling", "campus"]
    public static List<string> SplitTags(string? tags) =>
        string.IsNullOrWhiteSpace(tags)
            ? new List<string>()
            : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    // 1536 -> "1.5 KB"
    public static string FileSize(long bytes) =>
        bytes < 1024 ? $"{bytes} B"
        : bytes < 1024 * 1024 ? $"{bytes / 1024.0:0.#} KB"
        : $"{bytes / (1024.0 * 1024):0.#} MB";

    private static string Plural(int n, string unit) => n == 1 ? $"1 {unit} ago" : $"{n} {unit}s ago";
}
