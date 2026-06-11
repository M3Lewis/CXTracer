using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace CXTracer.Models;

public sealed partial class DisplayEvent
{
    // Thread-safe brush instances using ImmutableSolidColorBrush
    private static readonly IBrush BgUser = new ImmutableSolidColorBrush(Color.Parse("#EAF7F0"));
    private static readonly IBrush BgAssistant = new ImmutableSolidColorBrush(Color.Parse("#EDF4FF"));
    private static readonly IBrush BgFinal = new ImmutableSolidColorBrush(Color.Parse("#FFF2E8"));
    private static readonly IBrush BgCommand = new ImmutableSolidColorBrush(Color.Parse("#F4F1FF"));
    private static readonly IBrush BgCommandOutput = new ImmutableSolidColorBrush(Color.Parse("#F7F5EF"));
    private static readonly IBrush BgDiff = new ImmutableSolidColorBrush(Color.Parse("#EEF8F4"));
    private static readonly IBrush BgTool = new ImmutableSolidColorBrush(Color.Parse("#F6F0FA"));
    private static readonly IBrush BgError = new ImmutableSolidColorBrush(Color.Parse("#FFF0F0"));
    private static readonly IBrush BgDefault = new ImmutableSolidColorBrush(Color.Parse("#FAFAFA"));

    private static readonly IBrush BdrNavActive = new ImmutableSolidColorBrush(Color.Parse("#E97924"));
    private static readonly IBrush BdrUser = new ImmutableSolidColorBrush(Color.Parse("#A8DEC2"));
    private static readonly IBrush BdrAssistant = new ImmutableSolidColorBrush(Color.Parse("#AFCDF5"));
    private static readonly IBrush BdrFinal = new ImmutableSolidColorBrush(Color.Parse("#F2C49E"));
    private static readonly IBrush BdrCommand = new ImmutableSolidColorBrush(Color.Parse("#CFC6F4"));
    private static readonly IBrush BdrCommandOutput = new ImmutableSolidColorBrush(Color.Parse("#DDD5C8"));
    private static readonly IBrush BdrDiff = new ImmutableSolidColorBrush(Color.Parse("#B6DED0"));
    private static readonly IBrush BdrTool = new ImmutableSolidColorBrush(Color.Parse("#D7BDE6"));
    private static readonly IBrush BdrError = new ImmutableSolidColorBrush(Color.Parse("#F0B7B7"));
    private static readonly IBrush BdrDefault = new ImmutableSolidColorBrush(Color.Parse("#E0E0E0"));

    private static readonly IBrush BadgeBgUser = new ImmutableSolidColorBrush(Color.Parse("#CFF1DD"));
    private static readonly IBrush BadgeBgAssistant = new ImmutableSolidColorBrush(Color.Parse("#D6E8FF"));
    private static readonly IBrush BadgeBgFinal = new ImmutableSolidColorBrush(Color.Parse("#FFE0C7"));
    private static readonly IBrush BadgeBgCommand = new ImmutableSolidColorBrush(Color.Parse("#E4DFFF"));
    private static readonly IBrush BadgeBgCommandOutput = new ImmutableSolidColorBrush(Color.Parse("#ECE5DA"));
    private static readonly IBrush BadgeBgDiff = new ImmutableSolidColorBrush(Color.Parse("#D8F1E8"));
    private static readonly IBrush BadgeBgTool = new ImmutableSolidColorBrush(Color.Parse("#EBD9F5"));
    private static readonly IBrush BadgeBgError = new ImmutableSolidColorBrush(Color.Parse("#FFD6D6"));
    private static readonly IBrush BadgeBgDefault = new ImmutableSolidColorBrush(Color.Parse("#EFEFEF"));

    private static readonly IBrush BadgeFgUser = new ImmutableSolidColorBrush(Color.Parse("#1E6B45"));
    private static readonly IBrush BadgeFgAssistant = new ImmutableSolidColorBrush(Color.Parse("#245A9C"));
    private static readonly IBrush BadgeFgFinal = new ImmutableSolidColorBrush(Color.Parse("#A4541F"));
    private static readonly IBrush BadgeFgCommand = new ImmutableSolidColorBrush(Color.Parse("#5740A8"));
    private static readonly IBrush BadgeFgCommandOutput = new ImmutableSolidColorBrush(Color.Parse("#6B5D4A"));
    private static readonly IBrush BadgeFgDiff = new ImmutableSolidColorBrush(Color.Parse("#23765A"));
    private static readonly IBrush BadgeFgTool = new ImmutableSolidColorBrush(Color.Parse("#7A3C9A"));
    private static readonly IBrush BadgeFgError = new ImmutableSolidColorBrush(Color.Parse("#A33131"));
    private static readonly IBrush BadgeFgDefault = new ImmutableSolidColorBrush(Color.Parse("#555555"));

    // Syntax-highlighting diff brushes
    private static readonly IBrush GreenFg = new ImmutableSolidColorBrush(Color.Parse("#2E7D32"));
    private static readonly IBrush GreenBg = new ImmutableSolidColorBrush(Color.Parse("#E8F5E9"));
    private static readonly IBrush RedFg = new ImmutableSolidColorBrush(Color.Parse("#C62828"));
    private static readonly IBrush RedBg = new ImmutableSolidColorBrush(Color.Parse("#FFEBEE"));
    private static readonly IBrush TealFg = new ImmutableSolidColorBrush(Color.Parse("#00796B"));
    private static readonly IBrush TealBg = new ImmutableSolidColorBrush(Color.Parse("#E0F2F1"));
    private static readonly IBrush DefaultFg = new ImmutableSolidColorBrush(Color.Parse("#333333"));
    private static readonly IBrush DefaultBg = new ImmutableSolidColorBrush(Color.Parse("#F9F9F9"));

    private static readonly Thickness ThicknessBorder1 = new(1);
    private static readonly Thickness ThicknessBorder2 = new(2);

    public IBrush CardBackground => Kind switch
    {
        EventKind.User => BgUser,
        EventKind.Assistant => BgAssistant,
        EventKind.Final => BgFinal,
        EventKind.Command => BgCommand,
        EventKind.CommandOutput => BgCommandOutput,
        EventKind.Diff => BgDiff,
        EventKind.Tool => BgTool,
        EventKind.ToolCall => BgTool,
        EventKind.ToolResult => BgTool,
        EventKind.Error => BgError,
        _ => BgDefault
    };

    public IBrush CardBorder => IsCurrentNavigationTarget
        ? BdrNavActive
        : Kind switch
        {
            EventKind.User => BdrUser,
            EventKind.Assistant => BdrAssistant,
            EventKind.Final => BdrFinal,
            EventKind.Command => BdrCommand,
            EventKind.CommandOutput => BdrCommandOutput,
            EventKind.Diff => BdrDiff,
            EventKind.Tool => BdrTool,
            EventKind.ToolCall => BdrTool,
            EventKind.ToolResult => BdrTool,
            EventKind.Error => BdrError,
            _ => BdrDefault
        };

    public Thickness CardBorderThickness => IsCurrentNavigationTarget
        ? ThicknessBorder2
        : ThicknessBorder1;

    public IBrush RoleBadgeBackground => Kind switch
    {
        EventKind.User => BadgeBgUser,
        EventKind.Assistant => BadgeBgAssistant,
        EventKind.Final => BadgeBgFinal,
        EventKind.Command => BadgeBgCommand,
        EventKind.CommandOutput => BadgeBgCommandOutput,
        EventKind.Diff => BadgeBgDiff,
        EventKind.Tool => BadgeBgTool,
        EventKind.ToolCall => BadgeBgTool,
        EventKind.ToolResult => BadgeBgTool,
        EventKind.Error => BadgeBgError,
        _ => BadgeBgDefault
    };

    public IBrush RoleBadgeForeground => Kind switch
    {
        EventKind.User => BadgeFgUser,
        EventKind.Assistant => BadgeFgAssistant,
        EventKind.Final => BadgeFgFinal,
        EventKind.Command => BadgeFgCommand,
        EventKind.CommandOutput => BadgeFgCommandOutput,
        EventKind.Diff => BadgeFgDiff,
        EventKind.Tool => BadgeFgTool,
        EventKind.ToolCall => BadgeFgTool,
        EventKind.ToolResult => BadgeFgTool,
        EventKind.Error => BadgeFgError,
        _ => BadgeFgDefault
    };

    public string RoleLabel => Kind switch
    {
        EventKind.User => "User",
        EventKind.Assistant => "Assistant",
        EventKind.Final => "Final",
        EventKind.Command => "Command",
        EventKind.CommandOutput => "Output",
        EventKind.Diff => "Diff",
        EventKind.Tool => "Tool",
        EventKind.ToolCall => "Tool Call",
        EventKind.ToolResult => "Tool Output",
        EventKind.Error => "Error",
        _ => Kind.ToString()
    };
}
