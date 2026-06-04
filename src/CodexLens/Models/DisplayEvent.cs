using Avalonia.Media;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace CodexLens.Models;

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

    private static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));

    public IBrush CardBackground => Kind switch
    {
        EventKind.User => Brush("#EAF7F0"),          // 清淡薄荷绿
        EventKind.Assistant => Brush("#EDF4FF"),     // 清淡蓝
        EventKind.Final => Brush("#FFF2E8"),         // 清淡蜜桃橙
        EventKind.Command => Brush("#F4F1FF"),       // 清淡薰衣草紫
        EventKind.CommandOutput => Brush("#F7F5EF"), // 米白
        EventKind.Diff => Brush("#EEF8F4"),          // 清淡青绿
        EventKind.Tool => Brush("#F6F0FA"),          // 淡紫
        EventKind.Error => Brush("#FFF0F0"),         // 淡红
        _ => Brush("#FAFAFA")
    };

    public IBrush CardBorder => IsCurrentNavigationTarget
        ? Brush("#E97924")
        : Kind switch
    {
        EventKind.User => Brush("#A8DEC2"),
        EventKind.Assistant => Brush("#AFCDF5"),
        EventKind.Final => Brush("#F2C49E"),
        EventKind.Command => Brush("#CFC6F4"),
        EventKind.CommandOutput => Brush("#DDD5C8"),
        EventKind.Diff => Brush("#B6DED0"),
        EventKind.Tool => Brush("#D7BDE6"),
        EventKind.Error => Brush("#F0B7B7"),
        _ => Brush("#E0E0E0")
    };

    public Thickness CardBorderThickness => IsCurrentNavigationTarget
        ? new Thickness(3)
        : new Thickness(1);

    public IBrush RoleBadgeBackground => Kind switch
    {
        EventKind.User => Brush("#CFF1DD"),
        EventKind.Assistant => Brush("#D6E8FF"),
        EventKind.Final => Brush("#FFE0C7"),
        EventKind.Command => Brush("#E4DFFF"),
        EventKind.CommandOutput => Brush("#ECE5DA"),
        EventKind.Diff => Brush("#D8F1E8"),
        EventKind.Tool => Brush("#EBD9F5"),
        EventKind.Error => Brush("#FFD6D6"),
        _ => Brush("#EFEFEF")
    };

    public IBrush RoleBadgeForeground => Kind switch
    {
        EventKind.User => Brush("#1E6B45"),
        EventKind.Assistant => Brush("#245A9C"),
        EventKind.Final => Brush("#A4541F"),
        EventKind.Command => Brush("#5740A8"),
        EventKind.CommandOutput => Brush("#6B5D4A"),
        EventKind.Diff => Brush("#23765A"),
        EventKind.Tool => Brush("#7A3C9A"),
        EventKind.Error => Brush("#A33131"),
        _ => Brush("#555555")
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
