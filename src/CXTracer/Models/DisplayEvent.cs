using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Media;
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
        if (rawImg == null && !string.IsNullOrWhiteSpace(RawJson))
        {
            rawImg = ExtractImagePathFromJson(RawJson);
        }

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
