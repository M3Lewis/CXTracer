using Avalonia.Media;

namespace CXTracer.Models;

public sealed class DiffLine
{
    public required string Text { get; init; }
    public required IBrush Foreground { get; init; }
    public required IBrush Background { get; init; }
}
