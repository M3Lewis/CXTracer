using Avalonia.Media;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace CXTracer.Models;

public sealed partial class DisplayEvent : ObservableObject
{
    public required string Id { get; init; }
    public required int LineNumber { get; init; }
    public required EventPane Pane { get; init; }
    public required EventKind Kind { get; init; }
    public required string Title { get; init; }
    public required string Text { get; init; }
    public required string RawJson { get; init; }
    public DateTimeOffset? Timestamp { get; init; }

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isCurrentNavigationTarget;

    public string TimeText => Timestamp?.ToLocalTime().ToString("HH:mm:ss") ?? $"#{LineNumber}";
    public bool IsError => Kind == EventKind.Error || Text.Contains("error", StringComparison.OrdinalIgnoreCase) || Text.Contains("exception", StringComparison.OrdinalIgnoreCase);
    public bool IsDiff => Kind == EventKind.Diff;
    public bool IsCommand => Kind is EventKind.Command or EventKind.CommandOutput;
    public bool IsFinal => Kind == EventKind.Final;
    public bool IsRaw => Pane == EventPane.Raw || Kind == EventKind.Raw;
    public string KindText => Kind.ToString();
    public string Header => $"{TimeText} · {KindText}";

    // Cached brush instances — avoid per-access allocation + Color.Parse overhead.
    private static readonly IBrush BgUser = new SolidColorBrush(Color.Parse("#EAF7F0"));
    private static readonly IBrush BgAssistant = new SolidColorBrush(Color.Parse("#EDF4FF"));
    private static readonly IBrush BgFinal = new SolidColorBrush(Color.Parse("#FFF2E8"));
    private static readonly IBrush BgCommand = new SolidColorBrush(Color.Parse("#F4F1FF"));
    private static readonly IBrush BgCommandOutput = new SolidColorBrush(Color.Parse("#F7F5EF"));
    private static readonly IBrush BgDiff = new SolidColorBrush(Color.Parse("#EEF8F4"));
    private static readonly IBrush BgTool = new SolidColorBrush(Color.Parse("#F6F0FA"));
    private static readonly IBrush BgError = new SolidColorBrush(Color.Parse("#FFF0F0"));
    private static readonly IBrush BgDefault = new SolidColorBrush(Color.Parse("#FAFAFA"));

    private static readonly IBrush BdrNavActive = new SolidColorBrush(Color.Parse("#E97924"));
    private static readonly IBrush BdrUser = new SolidColorBrush(Color.Parse("#A8DEC2"));
    private static readonly IBrush BdrAssistant = new SolidColorBrush(Color.Parse("#AFCDF5"));
    private static readonly IBrush BdrFinal = new SolidColorBrush(Color.Parse("#F2C49E"));
    private static readonly IBrush BdrCommand = new SolidColorBrush(Color.Parse("#CFC6F4"));
    private static readonly IBrush BdrCommandOutput = new SolidColorBrush(Color.Parse("#DDD5C8"));
    private static readonly IBrush BdrDiff = new SolidColorBrush(Color.Parse("#B6DED0"));
    private static readonly IBrush BdrTool = new SolidColorBrush(Color.Parse("#D7BDE6"));
    private static readonly IBrush BdrError = new SolidColorBrush(Color.Parse("#F0B7B7"));
    private static readonly IBrush BdrDefault = new SolidColorBrush(Color.Parse("#E0E0E0"));

    private static readonly IBrush BadgeBgUser = new SolidColorBrush(Color.Parse("#CFF1DD"));
    private static readonly IBrush BadgeBgAssistant = new SolidColorBrush(Color.Parse("#D6E8FF"));
    private static readonly IBrush BadgeBgFinal = new SolidColorBrush(Color.Parse("#FFE0C7"));
    private static readonly IBrush BadgeBgCommand = new SolidColorBrush(Color.Parse("#E4DFFF"));
    private static readonly IBrush BadgeBgCommandOutput = new SolidColorBrush(Color.Parse("#ECE5DA"));
    private static readonly IBrush BadgeBgDiff = new SolidColorBrush(Color.Parse("#D8F1E8"));
    private static readonly IBrush BadgeBgTool = new SolidColorBrush(Color.Parse("#EBD9F5"));
    private static readonly IBrush BadgeBgError = new SolidColorBrush(Color.Parse("#FFD6D6"));
    private static readonly IBrush BadgeBgDefault = new SolidColorBrush(Color.Parse("#EFEFEF"));

    private static readonly IBrush BadgeFgUser = new SolidColorBrush(Color.Parse("#1E6B45"));
    private static readonly IBrush BadgeFgAssistant = new SolidColorBrush(Color.Parse("#245A9C"));
    private static readonly IBrush BadgeFgFinal = new SolidColorBrush(Color.Parse("#A4541F"));
    private static readonly IBrush BadgeFgCommand = new SolidColorBrush(Color.Parse("#5740A8"));
    private static readonly IBrush BadgeFgCommandOutput = new SolidColorBrush(Color.Parse("#6B5D4A"));
    private static readonly IBrush BadgeFgDiff = new SolidColorBrush(Color.Parse("#23765A"));
    private static readonly IBrush BadgeFgTool = new SolidColorBrush(Color.Parse("#7A3C9A"));
    private static readonly IBrush BadgeFgError = new SolidColorBrush(Color.Parse("#A33131"));
    private static readonly IBrush BadgeFgDefault = new SolidColorBrush(Color.Parse("#555555"));

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
        EventKind.Error => "Error",
        _ => Kind.ToString()
    };

    partial void OnIsCurrentNavigationTargetChanged(bool value)
    {
        OnPropertyChanged(nameof(CardBorder));
        OnPropertyChanged(nameof(CardBorderThickness));
    }
}
