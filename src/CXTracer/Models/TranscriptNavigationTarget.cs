namespace CXTracer.Models;

public sealed record TranscriptNavigationTarget(
    DisplayEvent Target,
    DisplayEvent? Companion);
