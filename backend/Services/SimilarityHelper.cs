// ============================================================
// MODULE : M5 / M8 — AI Intelligence support
// LAYER  : Service — pure helper, no dependencies
// FEATURE: F3 — AI Similar Idea Detection
// WHY THIS EXISTS:
//   requirements.pdf suggests pgvector for semantic search, but
//   Homebrew ships no pgvector build for PostgreSQL 16 (see
//   PROJECT_REFERENCE O7). Rather than block F3, embeddings are
//   stored as JSON on the Ideas table and cosine similarity is
//   computed here in C#.
//   This is exact, not approximate — pgvector's index is a speed
//   optimisation, not a correctness one. At course scale (hundreds
//   of ideas) a linear scan is fast enough. If the project later
//   moves to PostgreSQL 17, only the storage changes; this maths
//   stays identical.
// ============================================================
using System.Text.Json;

namespace AiInnovationHub.Api.Services;

public static class SimilarityHelper
{
    // ---- COSINE SIMILARITY ----
    // Returns 1.0 for identical direction, 0.0 for unrelated.
    // Formula: dot(a,b) / (|a| * |b|)
    public static double Cosine(float[] a, float[] b)
    {
        if (a.Length == 0 || b.Length == 0 || a.Length != b.Length) return 0;

        double dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot  += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        if (magA == 0 || magB == 0) return 0;   // guard against divide-by-zero
        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }

    // ---- SERIALISE for the EmbeddingJson column ----
    public static string Serialize(float[] vector) => JsonSerializer.Serialize(vector);

    // ---- DESERIALISE, tolerating null/corrupt values ----
    public static float[] Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<float>();
        try { return JsonSerializer.Deserialize<float[]>(json) ?? Array.Empty<float>(); }
        catch { return Array.Empty<float>(); }
    }
}
