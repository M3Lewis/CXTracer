using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;

namespace CXTracer.Models;

public sealed partial class SessionInfo : ObservableObject
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }

    [ObservableProperty]
    private DateTimeOffset _lastWriteTime;

    [ObservableProperty]
    private long _length;

    [ObservableProperty]
    private string _firstPrompt = string.Empty;

    [ObservableProperty]
    private string _projectHint = string.Empty;

    [ObservableProperty]
    private int _eventCount;

    public string DisplayTitle
    {
        get
        {
            var prompt = string.IsNullOrWhiteSpace(FirstPrompt) ? FileName : FirstPrompt.Trim();
            return prompt.Length <= 68 ? prompt : prompt[..68] + "...";
        }
    }

    public string DisplaySubtitle
    {
        get
        {
            var project = string.IsNullOrWhiteSpace(ProjectHint) ? "Unknown project" : ProjectHint;
            return $"{LastWriteTime.LocalDateTime:g} - {project}";
        }
    }

    public string StatusText
    {
        get
        {
            var age = DateTimeOffset.Now - LastWriteTime.ToLocalTime();
            if (age.TotalSeconds < 15) return "LIVE";
            if (age.TotalMinutes < 5) return "Active";
            return "History";
        }
    }

    partial void OnLastWriteTimeChanged(DateTimeOffset value)
    {
        OnPropertyChanged(nameof(DisplaySubtitle));
        OnPropertyChanged(nameof(StatusText));
    }

    partial void OnFirstPromptChanged(string value)
    {
        OnPropertyChanged(nameof(DisplayTitle));
    }

    partial void OnProjectHintChanged(string value)
    {
        OnPropertyChanged(nameof(DisplaySubtitle));
    }

    public void CopySummaryFrom(SessionInfo source)
    {
        LastWriteTime = source.LastWriteTime;
        Length = source.Length;
        FirstPrompt = source.FirstPrompt;
        ProjectHint = source.ProjectHint;
        EventCount = source.EventCount;
    }

    public static SessionInfo FromFile(FileInfo file)
    {
        return new SessionInfo
        {
            FilePath = file.FullName,
            FileName = file.Name,
            LastWriteTime = file.LastWriteTime,
            Length = file.Exists ? file.Length : 0
        };
    }
}
