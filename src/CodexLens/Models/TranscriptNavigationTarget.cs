namespace CodexLens.Models;

public sealed record TranscriptNavigationTarget(
    DisplayEvent Target,
    DisplayEvent? Companion);
