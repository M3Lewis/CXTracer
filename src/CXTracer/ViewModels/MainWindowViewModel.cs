using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SukiUI.Toasts;
using CXTracer.Models;
using CXTracer.Services;

namespace CXTracer.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    public ISukiToastManager ToastManager { get; } = new SukiToastManager();

    public event Action<DisplayEvent>? FilterAppliedScrollRequest;

    private const int SessionBatchSize = 40;
    private const int EventBatchSize = 40;

    private readonly Dictionary<string, CancellationTokenSource> _changeDebouncers = new(StringComparer.OrdinalIgnoreCase);
    private readonly SessionScanner _scanner;
    private readonly SessionReader _reader;
    private readonly SessionWatcher _watcher;
    private readonly AppSettingsService _settingsService;
    private readonly List<DisplayEvent> _allEvents = [];
    private List<DisplayEvent>? _sortedSyncEventsCache;
    private readonly List<SessionInfo> _allSessions = [];
    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _enrichCts;
    private CancellationTokenSource? _sessionFilterCts;
    private CancellationTokenSource? _eventFilterCts;
    private bool _selectionChanging;
    private bool _isLoadingSettings;
    private ShortcutGesture? _syncNavigationShortcut;
    private IResourceDictionary? _enUsDictionary;
    private IResourceDictionary? _zhCnDictionary;

    public ObservableCollection<SessionInfo> Sessions { get; } = [];
    public ObservableCollection<DisplayEvent> ConversationEvents { get; } = [];
    public ObservableCollection<DisplayEvent> ExecutionEvents { get; } = [];
    public ObservableCollection<DisplayEvent> RawEvents { get; } = [];
    private static readonly string[] FilterKeys = ["All", "Conversation", "Commands", "Errors", "Diffs", "Final", "Tools", "Raw"];
    public ObservableCollection<FilterOptionItem> FilterOptions { get; } = new(
        FilterKeys.Select(k => new FilterOptionItem(k, k)).ToArray());

    private static readonly Dictionary<string, string> FilterKeyToResourceKey = new()
    {
        ["All"] = "FilterAll",
        ["Conversation"] = "FilterConversation",
        ["Commands"] = "FilterCommands",
        ["Errors"] = "FilterErrors",
        ["Diffs"] = "FilterDiffs",
        ["Final"] = "FilterFinal",
        ["Tools"] = "FilterTools",
        ["Raw"] = "FilterRaw"
    };

    [ObservableProperty]
    private string _rootPath = SessionScanner.DefaultRootPath();

    [ObservableProperty]
    private string _sessionSearchText = string.Empty;

    [ObservableProperty]
    private string _eventSearchText = string.Empty;

    [ObservableProperty]
    private FilterOptionItem? _selectedFilter;

    [ObservableProperty]
    private string _currentLanguage = "en";

    [ObservableProperty]
    private bool _minimizeToTray;

    [ObservableProperty]
    private bool _closeToTray;

    [ObservableProperty]
    private SessionInfo? _selectedSession;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _showRawEvents;

    [ObservableProperty]
    private bool _pinSelectedSession = true;

    [ObservableProperty]
    private EventPane _activeTranscriptPane = EventPane.Conversation;

    [ObservableProperty]
    private DisplayEvent? _currentTranscriptEvent;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmSyncShortcutCommand))]
    private bool _isCapturingSyncShortcut;

    [ObservableProperty]
    private bool _isSynchronizedNavigationEnabled;

    [ObservableProperty]
    private bool _expandAllEventsByDefault;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmSyncShortcutCommand))]
    private string _pendingSyncShortcutText = string.Empty;

    [ObservableProperty]
    private int _totalEventCount;

    [ObservableProperty]
    private int _visibleEventCount;

    [ObservableProperty]
    private DisplayEvent? _detailPopupEvent;

    public bool IsDetailPopupOpen => DetailPopupEvent is not null;

    [ObservableProperty]
    private bool _isImageViewerOpen;

    [ObservableProperty]
    private string? _viewerImagePath;

    [ObservableProperty]
    private string _viewerImageStretch = "None";

    public string ViewerToggleText => ViewerImageStretch == "None" ? L("ViewerFitWindow", "Fit Window") : L("ViewerOriginalSize", "Original Size");

    public string SessionCountText => LF("SessionCountFormat", "{0} sessions", Sessions.Count);
    public string EventCountText => LF("EventCountFormat", "{0}/{1} events", VisibleEventCount, TotalEventCount);
    public string SelectedSessionPath => SelectedSession?.FilePath ?? L("NoSessionSelected", "No session selected");
    public bool IsConversationPaneActive => ActiveTranscriptPane == EventPane.Conversation;
    public bool IsExecutionPaneActive => ActiveTranscriptPane == EventPane.Execution;
    public string SyncNavigationShortcutText => _syncNavigationShortcut?.DisplayText ?? L("SyncNavUnset", "Unset");
    public string SyncShortcutEditorText => IsCapturingSyncShortcut
        ? L("SyncNavPressShortcut", "Press shortcut...")
        : string.IsNullOrWhiteSpace(PendingSyncShortcutText)
            ? SyncNavigationShortcutText
            : PendingSyncShortcutText;

    public MainWindowViewModel(SessionScanner scanner, SessionReader reader, SessionWatcher watcher, AppSettingsService settingsService)
    {
        _scanner = scanner;
        _reader = reader;
        _watcher = watcher;
        _settingsService = settingsService;
        _watcher.SessionFileChanged += OnSessionFileChanged;
        _selectedFilter = FilterOptions[0];
        LoadSettings();
        ApplyLanguage(_currentLanguage);
        _statusMessage = L("StatusReady", "Ready");
        _ = RefreshAsync();
    }

    // ── Shared helpers ──

    private static Task YieldToUiAsync()
    {
        return Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background).GetTask();
    }

    private static DateTimeOffset EventSortTimestamp(DisplayEvent evt)
    {
        return evt.Timestamp ?? DateTimeOffset.MinValue.AddTicks(Math.Max(0, evt.LineNumber));
    }

    private static int FindFirstIndex(IReadOnlyList<DisplayEvent> events, Func<DisplayEvent, bool> predicate)
    {
        for (var i = 0; i < events.Count; i++)
        {
            if (predicate(events[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindLastIndex(IReadOnlyList<DisplayEvent> events, Func<DisplayEvent, bool> predicate)
    {
        for (var i = events.Count - 1; i >= 0; i--)
        {
            if (predicate(events[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static string NormalizeRoot(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return SessionScanner.DefaultRootPath();
        }

        value = Environment.ExpandEnvironmentVariables(value.Trim().Trim('"'));
        return Path.GetFullPath(value);
    }

    private static bool PathsEqual(string a, string b)
    {
        return string.Equals(
            Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar),
            Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _enrichCts?.Cancel();
        _enrichCts?.Dispose();
        _sessionFilterCts?.Cancel();
        _sessionFilterCts?.Dispose();
        _eventFilterCts?.Cancel();
        _eventFilterCts?.Dispose();
        _watcher.SessionFileChanged -= OnSessionFileChanged;
        _watcher.Dispose();

        lock (_changeDebouncers)
        {
            foreach (var cts in _changeDebouncers.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }

            _changeDebouncers.Clear();
        }
    }
}
