using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using CXTracer.Services;

namespace CXTracer.Models;

public sealed partial class DisplayEvent : ObservableObject
{
    private static readonly Regex MarkdownImageRegex = new(@"!\[.*?\]\((.*?)\)", RegexOptions.Compiled);
    private static readonly Regex HtmlImageRegex = new(@"<img\s+[^>]*src=[""'](.*?)[""']", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public required string Id { get; init; }
    public required int LineNumber { get; init; }
    public required EventPane Pane { get; init; }
    public required EventKind Kind { get; init; }
    public required string Title { get; init; }
    public required string Text { get; init; }
    public required string RawJson { get; init; }
    public DateTimeOffset? Timestamp { get; init; }

    public string FormattedRawJson
    {
        get
        {
            if (string.IsNullOrWhiteSpace(RawJson)) return string.Empty;
            try
            {
                using var doc = JsonDocument.Parse(RawJson);
                return JsonSerializer.Serialize(doc.RootElement, AppJsonContext.Default.JsonElement);
            }
            catch
            {
                return RawJson;
            }
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    [NotifyPropertyChangedFor(nameof(IsDiffAndExpanded))]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isCurrentNavigationTarget;

    [ObservableProperty]
    private string _columnSequenceText = string.Empty;

    [ObservableProperty]
    private string _mergedSequenceText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    private bool _canExpand;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImage))]
    private string? _imagePath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AdditionsCount))]
    [NotifyPropertyChangedFor(nameof(DeletionsCount))]
    private List<DiffLine>? _diffLines;

    public string TimeText => Timestamp?.ToLocalTime().ToString("HH:mm:ss") ?? $"#{LineNumber}";
    public bool IsError => Kind == EventKind.Error || Text.Contains("error", StringComparison.OrdinalIgnoreCase) || Text.Contains("exception", StringComparison.OrdinalIgnoreCase);
    public bool IsDiff => Kind == EventKind.Diff;
    public bool IsCommand => Kind is EventKind.Command or EventKind.CommandOutput;
    public bool IsFinal => Kind == EventKind.Final;
    public bool IsRaw => Pane == EventPane.Raw || Kind == EventKind.Raw;
    public string KindText => Kind.ToString();
    public string Header => $"{TimeText} · {KindText}";

    public bool HasImage => !string.IsNullOrEmpty(ImagePath);
    public bool IsDiffAndExpanded => IsDiff && IsExpanded;
    public int AdditionsCount => DiffLines?.Count(l => l.Text.StartsWith('+') && !l.Text.StartsWith("+++")) ?? 0;
    public int DeletionsCount => DiffLines?.Count(l => l.Text.StartsWith('-') && !l.Text.StartsWith("---")) ?? 0;

    public string PreviewText
    {
        get
        {
            if (string.IsNullOrEmpty(Text)) return string.Empty;

            int lineLimitIndex = -1;
            int newlineCount = 0;
            for (int i = 0; i < Text.Length; i++)
            {
                if (Text[i] == '\n')
                {
                    newlineCount++;
                    if (newlineCount == 3)
                    {
                        lineLimitIndex = i;
                        break;
                    }
                }
            }

            int charLimitIndex = Text.Length > 200 ? 200 : -1;

            int limitIndex = -1;
            if (lineLimitIndex >= 0 && charLimitIndex >= 0)
            {
                limitIndex = Math.Min(lineLimitIndex, charLimitIndex);
            }
            else if (lineLimitIndex >= 0)
            {
                limitIndex = lineLimitIndex;
            }
            else if (charLimitIndex >= 0)
            {
                limitIndex = charLimitIndex;
            }

            if (limitIndex >= 0)
            {
                return Text[..limitIndex].TrimEnd() + " ...";
            }

            return Text;
        }
    }

    public string DisplayText => (CanExpand && !IsExpanded) ? PreviewText : Text;

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

    [RelayCommand]
    private void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
    }

    public void Initialize(string? sessionFilePath = null)
    {
        var lineCount = CountLines(Text);
        CanExpand = Kind is EventKind.ToolCall or EventKind.ToolResult or EventKind.Diff or EventKind.CommandOutput or EventKind.Error 
                    || Text.Length > 200 
                    || lineCount > 3;

        var rawImg = ExtractImagePath(Text);
        if (rawImg != null)
        {
            ImagePath = ResolvePath(rawImg, sessionFilePath);
        }

        ParseDiffLines();
        ResetExpansionState(false);
    }

    public void ResetExpansionState(bool expandAllByDefault)
    {
        if (expandAllByDefault)
        {
            IsExpanded = true;
        }
        else if (Pane == EventPane.Conversation)
        {
            IsExpanded = true;
        }
        else
        {
            var lineCount = CountLines(Text);
            IsExpanded = Text.Length <= 150 && lineCount <= 3;
        }
    }

    private static int CountLines(string? text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        int count = 1;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n') count++;
        }
        return count;
    }

    private static string? ExtractImagePath(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var mdMatch = MarkdownImageRegex.Match(text);
        if (mdMatch.Success)
        {
            return mdMatch.Groups[1].Value.Trim();
        }

        var htmlMatch = HtmlImageRegex.Match(text);
        if (htmlMatch.Success)
        {
            return htmlMatch.Groups[1].Value.Trim();
        }

        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                trimmed.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                trimmed.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                trimmed.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
                trimmed.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
            {
                if (!trimmed.Contains(' ') && (trimmed.Contains('/') || trimmed.Contains('\\') || trimmed.Contains('.')))
                {
                    return trimmed;
                }
            }
        }

        return null;
    }

    private static string ResolvePath(string rawPath, string? sessionFilePath)
    {
        if (string.IsNullOrWhiteSpace(rawPath)) return rawPath;

        if (rawPath.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) ||
            rawPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            rawPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return rawPath;
        }

        if (rawPath.StartsWith("~"))
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            rawPath = Path.Combine(userProfile, rawPath[1..].TrimStart('/', '\\'));
        }

        rawPath = rawPath.Replace('/', '\\');

        if (rawPath.StartsWith("\\mnt\\", StringComparison.OrdinalIgnoreCase))
        {
            var components = rawPath.Split('\\', StringSplitOptions.RemoveEmptyEntries);
            if (components.Length >= 3 && components[0].Equals("mnt", StringComparison.OrdinalIgnoreCase))
            {
                var driveLetter = components[1].ToUpperInvariant();
                if (driveLetter.Length == 1 && driveLetter[0] >= 'A' && driveLetter[0] <= 'Z')
                {
                    var subPath = string.Join('\\', components.Skip(2));
                    rawPath = $"{driveLetter}:\\{subPath}";
                }
            }
        }

        if (rawPath.StartsWith('\\') && !rawPath.StartsWith("\\\\"))
        {
            if (!string.IsNullOrEmpty(sessionFilePath))
            {
                if (sessionFilePath.StartsWith("\\\\wsl$\\") || sessionFilePath.StartsWith("\\\\wsl.localhost\\"))
                {
                    var parts = sessionFilePath.Split('\\', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var wslPrefix = $"\\\\{parts[0]}\\{parts[1]}";
                        rawPath = wslPrefix + rawPath;
                    }
                }
            }
        }

        if (!Path.IsPathRooted(rawPath) && !string.IsNullOrEmpty(sessionFilePath))
        {
            var sessionDir = Path.GetDirectoryName(sessionFilePath);
            if (!string.IsNullOrEmpty(sessionDir))
            {
                rawPath = Path.GetFullPath(Path.Combine(sessionDir, rawPath));
            }
        }

        return rawPath;
    }

    private void ParseDiffLines()
    {
        if (Kind != EventKind.Diff || string.IsNullOrEmpty(Text))
        {
            DiffLines = null;
            return;
        }

        var lines = Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var parsedLines = new List<DiffLine>(lines.Length);

        foreach (var line in lines)
        {
            IBrush fg = DefaultFg;
            IBrush bg = DefaultBg;

            if (line.StartsWith('+') && !line.StartsWith("+++"))
            {
                fg = GreenFg;
                bg = GreenBg;
            }
            else if (line.StartsWith('-') && !line.StartsWith("---"))
            {
                fg = RedFg;
                bg = RedBg;
            }
            else if (line.StartsWith("@@") || line.StartsWith("Index: ") || line.StartsWith("diff --git ") || line.StartsWith("--- ") || line.StartsWith("+++ "))
            {
                fg = TealFg;
                bg = TealBg;
            }

            parsedLines.Add(new DiffLine
            {
                Text = line,
                Foreground = fg,
                Background = bg
            });
        }

        DiffLines = parsedLines;
    }

    partial void OnIsCurrentNavigationTargetChanged(bool value)
    {
        OnPropertyChanged(nameof(CardBorder));
        OnPropertyChanged(nameof(CardBorderThickness));
    }
}
