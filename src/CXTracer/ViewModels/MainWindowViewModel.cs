using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SukiUI.Toasts;
using CXTracer.Models;
using CXTracer.Services;

namespace CXTracer.ViewModels;

public sealed partial class FilterOptionItem : ObservableObject
{
    public string Key { get; }

    [ObservableProperty]
    private string _displayName;

    public FilterOptionItem(string key, string displayName)
    {
        Key = key;
        _displayName = displayName;
    }

    public override string ToString() => DisplayName;
}

public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    public ISukiToastManager ToastManager { get; } = new SukiToastManager();

    private const int SessionBatchSize = 40;
    private const int EventBatchSize = 40;

    private readonly Dictionary<string, CancellationTokenSource> _changeDebouncers = new(StringComparer.OrdinalIgnoreCase);
    private readonly SessionScanner _scanner;
    private readonly SessionReader _reader;
    private readonly SessionWatcher _watcher;
    private readonly AppSettingsService _settingsService;
    private readonly List<DisplayEvent> _allEvents = [];
    private readonly List<SessionInfo> _allSessions = [];
    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _enrichCts;
    private CancellationTokenSource? _sessionFilterCts;
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
    [NotifyCanExecuteChangedFor(nameof(ConfirmSyncShortcutCommand))]
    private string _pendingSyncShortcutText = string.Empty;

    [ObservableProperty]
    private int _totalEventCount;

    [ObservableProperty]
    private int _visibleEventCount;

    [ObservableProperty]
    private DisplayEvent? _detailPopupEvent;

    public bool IsDetailPopupOpen => DetailPopupEvent is not null;

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

    partial void OnRootPathChanged(string value)
    {
        StatusMessage = Directory.Exists(value)
            ? L("StatusDirExists", "Directory exists. Click Refresh to scan.")
            : LF("StatusDirNotFound", "Directory not found: {0}", value);
    }

    partial void OnSelectedSessionChanged(SessionInfo? value)
    {
        OnPropertyChanged(nameof(SelectedSessionPath));

        if (_selectionChanging || value is null)
        {
            return;
        }

        _ = LoadSelectedSessionAsync(value);
    }

    partial void OnSessionSearchTextChanged(string value)
    {
        _sessionFilterCts?.Cancel();
        _sessionFilterCts = new CancellationTokenSource();
        var ct = _sessionFilterCts.Token;
        _ = FilterSessionsAndEnrichAsync(ct);
    }

    partial void OnEventSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedFilterChanged(FilterOptionItem? value) => ApplyFilter();
    partial void OnShowRawEventsChanged(bool value) => ApplyFilter();

    partial void OnCurrentLanguageChanged(string value)
    {
        ApplyLanguage(value);
        if (!_isLoadingSettings)
        {
            SaveSettings();
        }
    }
    partial void OnActiveTranscriptPaneChanged(EventPane value)
    {
        OnPropertyChanged(nameof(IsConversationPaneActive));
        OnPropertyChanged(nameof(IsExecutionPaneActive));
    }

    partial void OnCurrentTranscriptEventChanged(DisplayEvent? oldValue, DisplayEvent? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.IsCurrentNavigationTarget = false;
        }

        if (newValue is not null)
        {
            newValue.IsCurrentNavigationTarget = true;
            SetActiveTranscriptPane(newValue.Pane);
        }
    }

    partial void OnIsCapturingSyncShortcutChanged(bool value) => OnPropertyChanged(nameof(SyncShortcutEditorText));
    partial void OnPendingSyncShortcutTextChanged(string value) => OnPropertyChanged(nameof(SyncShortcutEditorText));
    partial void OnDetailPopupEventChanged(DisplayEvent? value) => OnPropertyChanged(nameof(IsDetailPopupOpen));

    public void ShowDetailPopup(DisplayEvent evt) => DetailPopupEvent = evt;
    public void CloseDetailPopup() => DetailPopupEvent = null;
    partial void OnIsSynchronizedNavigationEnabledChanged(bool value)
    {
        if (!_isLoadingSettings)
        {
            SaveSettings();
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = L("StatusScanning", "Scanning sessions...");
            ResetEvents();
            Sessions.Clear();
            _allSessions.Clear();
            OnPropertyChanged(nameof(SessionCountText));

            var normalized = NormalizeRoot(RootPath);
            RootPath = normalized;

            if (!Directory.Exists(normalized))
            {
                StatusMessage = $"Directory not found: {normalized}";
                _watcher.Stop();
                return;
            }

            var sessions = await Task.Run(() => _scanner.ScanLight(normalized)).ConfigureAwait(true);
            _allSessions.AddRange(sessions);
            await PopulateSessionsFilteredAsync(CancellationToken.None).ConfigureAwait(true);

            _enrichCts?.Cancel();
            _enrichCts = new CancellationTokenSource();
            _ = StartBackgroundEnrichmentAsync(_enrichCts.Token);

            _watcher.Start(normalized);
            StatusMessage = sessions.Count == 0
                ? L("StatusNoSessions", "No .jsonl sessions found.")
                : LF("StatusLoadedSessions", "Loaded {0} session entries.", sessions.Count);

        }
        catch (Exception ex)
        {
            StatusMessage = LF("StatusScanFailed", "Scan failed: {0}", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ClearSearch()
    {
        EventSearchText = string.Empty;
    }

    [RelayCommand]
    private void OpenDefaultRoot()
    {
        RootPath = SessionScanner.DefaultRootPath();
        _ = RefreshAsync();
    }

    [RelayCommand]
    private void ToggleRaw()
    {
        ShowRawEvents = !ShowRawEvents;
    }

    [RelayCommand]
    private void ConfirmSyncShortcut()
    {
        ConfirmPendingSyncShortcut();
    }

    public void ConfirmPendingSyncShortcut()
    {
        var gesture = ParseShortcutText(PendingSyncShortcutText);
        if (gesture is null)
        {
            StatusMessage = L("SyncNavChooseFirst", "Choose a Ctrl/Shift/Alt + key shortcut first.");
            return;
        }

        _syncNavigationShortcut = gesture;
        PendingSyncShortcutText = string.Empty;
        IsCapturingSyncShortcut = false;
        OnPropertyChanged(nameof(SyncNavigationShortcutText));
        OnPropertyChanged(nameof(SyncShortcutEditorText));
        SaveSettings();
        StatusMessage = LF("SyncNavSet", "Sync navigation shortcut set to {0}.", gesture.DisplayText);
    }

    private bool CanConfirmSyncShortcut()
    {
        return CanConfirmPendingSyncShortcut();
    }

    public bool CanConfirmPendingSyncShortcut()
    {
        return ParseShortcutText(PendingSyncShortcutText) is not null;
    }

    public void StartSyncShortcutCapture()
    {
        PendingSyncShortcutText = string.Empty;
        IsCapturingSyncShortcut = true;
        StatusMessage = L("SyncNavPressKey", "Press Ctrl/Shift/Alt + a key for sync navigation.");
    }

    public void CaptureSyncShortcut(bool ctrl, bool shift, bool alt, string keyText)
    {
        var gesture = ShortcutGesture.Create(ctrl, shift, alt, keyText);
        if (!gesture.IsValid)
        {
            RejectSyncShortcutCapture("Shortcut must be Ctrl/Shift/Alt + another key.");
            return;
        }

        PendingSyncShortcutText = gesture.DisplayText;
        IsCapturingSyncShortcut = false;
        StatusMessage = LF("SyncNavCaptured", "Captured {0}. Click OK to save.", gesture.DisplayText);
    }

    public void RejectSyncShortcutCapture(string message)
    {
        IsCapturingSyncShortcut = false;
        StatusMessage = message;
    }

    public bool MatchesSyncNavigationShortcut(bool ctrl, bool shift, bool alt, string keyText)
    {
        return _syncNavigationShortcut?.Matches(ctrl, shift, alt, keyText) == true;
    }

    public void ToggleSynchronizedNavigation()
    {
        IsSynchronizedNavigationEnabled = !IsSynchronizedNavigationEnabled;
        StatusMessage = IsSynchronizedNavigationEnabled
            ? L("SyncNavEnabled", "Synchronized navigation enabled.")
            : L("SyncNavDisabled", "Synchronized navigation disabled.");
    }

    public void SetActiveTranscriptPane(EventPane pane)
    {
        if (pane is EventPane.Conversation or EventPane.Execution)
        {
            ActiveTranscriptPane = pane;
        }
    }

    public void SetCurrentTranscriptEvent(DisplayEvent? evt)
    {
        if (evt is null || evt.Pane is EventPane.Conversation or EventPane.Execution)
        {
            CurrentTranscriptEvent = evt;
        }
    }

    public TranscriptNavigationTarget? GetSynchronizedNavigationTarget(
        EventPane requestedPane,
        int direction,
        DisplayEvent? anchor)
    {
        var events = ConversationEvents
            .Concat(ExecutionEvents)
            .OrderBy(EventSortTimestamp)
            .ThenBy(x => x.LineNumber)
            .ToList();

        if (events.Count == 0)
        {
            return null;
        }

        var effectiveAnchor = CurrentTranscriptEvent is not null
            && CurrentTranscriptEvent.Pane == requestedPane
            && events.Contains(CurrentTranscriptEvent)
            ? CurrentTranscriptEvent
            : anchor;
        var anchorIndex = GetAnchorIndex(events, effectiveAnchor, direction);
        var targetIndex = Math.Clamp(anchorIndex + Math.Sign(direction), 0, events.Count - 1);
        var target = events[targetIndex];
        SetCurrentTranscriptEvent(target);

        var companion = GetCompanionEvent(target);
        return new TranscriptNavigationTarget(target, companion);
    }

    private async Task LoadSelectedSessionAsync(SessionInfo session)
    {
        _loadCts?.Cancel();

        var cts = new CancellationTokenSource();
        _loadCts = cts;
        var ct = cts.Token;

        try
        {
            IsBusy = true;
            StatusMessage = LF("StatusReading", "Reading {0}...", session.FileName);
            ResetEvents();

            var summary = await Task.Run(() => _scanner.TryGetSessionSummary(session.FilePath), ct).ConfigureAwait(true);
            ct.ThrowIfCancellationRequested();
            if (summary is not null)
            {
                session.CopySummaryFrom(summary);
                // CopySummaryFrom sets IsEnriched = true, so background loop will skip this session
            }

            var events = await _reader.ReadAllAsync(session.FilePath, ct).ConfigureAwait(true);
            ct.ThrowIfCancellationRequested();

            _allEvents.Clear();
            _allEvents.AddRange(events);
            TotalEventCount = _allEvents.Count;
            await PopulateVisibleEventsAsync(_allEvents, ct).ConfigureAwait(true);
            StatusMessage = LF("StatusViewing", "Viewing {0}", session.FileName);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            StatusMessage = LF("StatusReadFailed", "Read failed: {0}", ex.Message);
        }
        finally
        {
            if (ReferenceEquals(_loadCts, cts))
            {
                IsBusy = false;
                _loadCts = null;
            }

            cts.Dispose();
        }
    }

    private void OnSessionFileChanged(object? sender, SessionFileChangedEventArgs e)
    {
        CancellationTokenSource cts;

        lock (_changeDebouncers)
        {
            if (_changeDebouncers.TryGetValue(e.Path, out var old))
            {
                old.Cancel();
                old.Dispose();
            }

            cts = new CancellationTokenSource();
            _changeDebouncers[e.Path] = cts;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(150, cts.Token).ConfigureAwait(false);

                await Dispatcher.UIThread.InvokeAsync(
                    async () => await HandleSessionFileChangedOnUiThreadAsync(e.Path));
            }
            catch (OperationCanceledException)
            {
            }
        });
    }

    private async Task HandleSessionFileChangedOnUiThreadAsync(string path)
    {
        try
        {
            UpsertSession(path);

            if (SelectedSession is null || !PathsEqual(path, SelectedSession.FilePath))
            {
                if (!PinSelectedSession)
                {
                    var target = Sessions.FirstOrDefault(s => PathsEqual(s.FilePath, path));
                    if (target is null)
                    {
                        var targetAll = _allSessions.FirstOrDefault(s => PathsEqual(s.FilePath, path));
                        if (targetAll is not null)
                        {
                            SessionSearchText = string.Empty;
                            target = Sessions.FirstOrDefault(s => PathsEqual(s.FilePath, path));
                        }
                    }
                    if (target is not null)
                    {
                        SelectedSession = target;
                    }
                }
                else
                {
                    StatusMessage = LF("StatusOtherSessionChanged", "Another session changed: {0}", Path.GetFileName(path));
                }
                return;
            }

            var events = await _reader.ReadAppendedAsync(path).ConfigureAwait(true);
            if (events.Count == 0)
            {
                return;
            }

            foreach (var evt in events)
            {
                _allEvents.Add(evt);
                AddIfVisible(evt);
            }

            TotalEventCount = _allEvents.Count;
            UpdateVisibleEventCount();
            StatusMessage = LF("StatusLiveUpdate", "Live update: {0} events", events.Count);
        }
        catch (Exception ex)
        {
            StatusMessage = LF("StatusLiveReadFailed", "Live read failed: {0}", ex.Message);
        }
    }

    private void UpsertSession(string path)
    {
        var info = _scanner.TryGetSession(path);
        if (info is null)
        {
            return;
        }

        // 1. Update the master list
        var existingAll = _allSessions.FirstOrDefault(s => PathsEqual(s.FilePath, info.FilePath));
        SessionInfo targetSession;
        if (existingAll is null)
        {
            _allSessions.Insert(0, info);
            targetSession = info;
            _ = EnrichSingleSessionAsync(info);
        }
        else
        {
            existingAll.LastWriteTime = info.LastWriteTime;
            existingAll.Length = info.Length;
            targetSession = existingAll;

            _allSessions.Remove(existingAll);
            int targetIdx = 0;
            while (targetIdx < _allSessions.Count && _allSessions[targetIdx].LastWriteTime > existingAll.LastWriteTime)
            {
                targetIdx++;
            }
            _allSessions.Insert(targetIdx, existingAll);
        }

        // 2. Incrementally update the visible collection
        bool matchesFilter = MatchesSessionFilter(targetSession);
        var existingVisible = Sessions.FirstOrDefault(s => PathsEqual(s.FilePath, targetSession.FilePath));

        if (matchesFilter)
        {
            if (existingVisible is null)
            {
                // Find correct position in Sessions
                int targetIdx = 0;
                while (targetIdx < Sessions.Count && Sessions[targetIdx].LastWriteTime > targetSession.LastWriteTime)
                {
                    targetIdx++;
                }
                Sessions.Insert(targetIdx, targetSession);
            }
            else
            {
                existingVisible.LastWriteTime = targetSession.LastWriteTime;
                existingVisible.Length = targetSession.Length;

                int oldIndex = Sessions.IndexOf(existingVisible);
                int targetIndex = 0;
                while (targetIndex < Sessions.Count && Sessions[targetIndex].LastWriteTime > existingVisible.LastWriteTime)
                {
                    targetIndex++;
                }

                if (targetIndex > oldIndex) targetIndex--;

                if (oldIndex != targetIndex)
                {
                    _selectionChanging = true;
                    var currentSelection = SelectedSession;
                    Sessions.RemoveAt(oldIndex);
                    Sessions.Insert(targetIndex, existingVisible);
                    SelectedSession = currentSelection;
                    _selectionChanging = false;
                }
            }
        }
        else
        {
            if (existingVisible is not null)
            {
                _selectionChanging = true;
                var currentSelection = SelectedSession;
                Sessions.Remove(existingVisible);
                if (currentSelection != null && PathsEqual(currentSelection.FilePath, existingVisible.FilePath))
                {
                    SelectedSession = Sessions.FirstOrDefault();
                }
                else
                {
                    SelectedSession = currentSelection;
                }
                _selectionChanging = false;
            }
        }

        OnPropertyChanged(nameof(SessionCountText));
    }

    private bool MatchesSessionFilter(SessionInfo s)
    {
        var query = SessionSearchText?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return (s.DisplayTitle != null && s.DisplayTitle.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
               (s.DisplaySubtitle != null && s.DisplaySubtitle.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
               (s.FilePath != null && s.FilePath.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private async Task PopulateSessionsFilteredAsync(CancellationToken cancellationToken)
    {
        var currentSelectionPath = SelectedSession?.FilePath;
        _selectionChanging = true;
        Sessions.Clear();

        var query = SessionSearchText?.Trim();
        var filtered = string.IsNullOrWhiteSpace(query)
            ? _allSessions
            : _allSessions.Where(MatchesSessionFilter).ToList();

        var count = 0;
        foreach (var session in filtered)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            Sessions.Add(session);
            count++;

            if (count % SessionBatchSize == 0)
            {
                OnPropertyChanged(nameof(SessionCountText));
                await YieldToUiAsync().ConfigureAwait(true);
            }
        }

        if (SelectedSession != null && Sessions.Any(s => PathsEqual(s.FilePath, SelectedSession.FilePath)))
        {
            // Keep selected
        }
        else
        {
            SelectedSession = Sessions.FirstOrDefault();
        }

        _selectionChanging = false;
        OnPropertyChanged(nameof(SessionCountText));

        if (SelectedSession != null && (currentSelectionPath == null || !PathsEqual(SelectedSession.FilePath, currentSelectionPath)))
        {
            await LoadSelectedSessionAsync(SelectedSession).ConfigureAwait(true);
        }
    }

    private async Task FilterSessionsAndEnrichAsync(CancellationToken ct)
    {
        try
        {
            await PopulateSessionsFilteredAsync(ct).ConfigureAwait(true);
            if (ct.IsCancellationRequested) return;

            _enrichCts?.Cancel();
            _enrichCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _ = StartBackgroundEnrichmentAsync(_enrichCts.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void ApplyFilter()
    {
        SetCurrentTranscriptEvent(null);
        ConversationEvents.Clear();
        ExecutionEvents.Clear();
        RawEvents.Clear();

        foreach (var evt in _allEvents)
        {
            AddIfVisible(evt);
        }

        UpdateVisibleEventCount();
    }

    private async Task AddSessionsAsync(IReadOnlyList<SessionInfo> sessions)
    {
        var count = 0;
        foreach (var session in sessions)
        {
            Sessions.Add(session);
            count++;

            if (count % SessionBatchSize == 0)
            {
                OnPropertyChanged(nameof(SessionCountText));
                await YieldToUiAsync().ConfigureAwait(true);
            }
        }

        OnPropertyChanged(nameof(SessionCountText));
    }

    private async Task PopulateVisibleEventsAsync(
        IEnumerable<DisplayEvent> events,
        CancellationToken cancellationToken)
    {
        ConversationEvents.Clear();
        ExecutionEvents.Clear();
        RawEvents.Clear();
        UpdateVisibleEventCount();

        var count = 0;
        foreach (var evt in events)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddIfVisible(evt);
            count++;

            if (count % EventBatchSize == 0)
            {
                UpdateVisibleEventCount();
                await YieldToUiAsync().ConfigureAwait(true);
            }
        }

        UpdateVisibleEventCount();
    }

    private static Task YieldToUiAsync()
    {
        return Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background).GetTask();
    }

    private void ResetEvents()
    {
        _allEvents.Clear();
        ConversationEvents.Clear();
        ExecutionEvents.Clear();
        RawEvents.Clear();
        SetCurrentTranscriptEvent(null);
        TotalEventCount = 0;
        VisibleEventCount = 0;
        OnPropertyChanged(nameof(EventCountText));
    }

    private void UpdateVisibleEventCount()
    {
        VisibleEventCount = ConversationEvents.Count + ExecutionEvents.Count + RawEvents.Count;
        OnPropertyChanged(nameof(EventCountText));
    }

    private void AddIfVisible(DisplayEvent evt)
    {
        if (!PassesFilter(evt))
        {
            return;
        }

        switch (evt.Pane)
        {
            case EventPane.Conversation:
                ConversationEvents.Add(evt);
                break;
            case EventPane.Execution:
                ExecutionEvents.Add(evt);
                break;
            case EventPane.Raw:
                if (ShowRawEvents || SelectedFilter?.Key == "Raw")
                {
                    RawEvents.Add(evt);
                }
                break;
        }
    }

    private bool PassesFilter(DisplayEvent evt)
    {
        if (!string.IsNullOrWhiteSpace(EventSearchText))
        {
            var q = EventSearchText.Trim();
            if (!evt.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                && !evt.Text.Contains(q, StringComparison.OrdinalIgnoreCase)
                && !evt.RawJson.Contains(q, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        var key = SelectedFilter?.Key;
        return key switch
        {
            "Conversation" => evt.Pane == EventPane.Conversation,
            "Commands" => evt.IsCommand,
            "Errors" => evt.IsError,
            "Diffs" => evt.IsDiff,
            "Final" => evt.IsFinal,
            "Tools" => evt.Kind == EventKind.Tool,
            "Raw" => evt.Pane == EventPane.Raw,
            _ => evt.Pane != EventPane.Raw || ShowRawEvents
        };
    }

    private void LoadSettings()
    {
        try
        {
            var settings = _settingsService.Load();
            _isLoadingSettings = true;
            IsSynchronizedNavigationEnabled = settings.IsSynchronizedNavigationEnabled;
            _syncNavigationShortcut = settings.SyncNavigationShortcut?.IsValid == true
                ? settings.SyncNavigationShortcut
                : null;

            var lang = settings.Language;
            if (string.IsNullOrWhiteSpace(lang))
            {
                lang = CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                    ? "zh"
                    : "en";
            }
            _currentLanguage = lang;
            _isLoadingSettings = false;

            OnPropertyChanged(nameof(SyncNavigationShortcutText));
            OnPropertyChanged(nameof(SyncShortcutEditorText));
            OnPropertyChanged(nameof(CurrentLanguage));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            _isLoadingSettings = false;
            StatusMessage = LF("StatusSettingsLoadFailed", "Settings load failed: {0}", ex.Message);
        }
    }

    private void SaveSettings()
    {
        try
        {
            _settingsService.Save(new AppSettings
            {
                IsSynchronizedNavigationEnabled = IsSynchronizedNavigationEnabled,
                SyncNavigationShortcut = _syncNavigationShortcut,
                Language = CurrentLanguage
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            StatusMessage = LF("StatusSettingsSaveFailed", "Settings save failed: {0}", ex.Message);
        }
    }

    // ── Localization helpers ──

    public void ApplyLanguage(string lang)
    {
        var app = Application.Current;
        if (app is null) return;

        var merged = app.Resources.MergedDictionaries;

        // Cache the pre-instantiated resource dictionaries from App.axaml on first run
        if (_enUsDictionary is null || _zhCnDictionary is null)
        {
            foreach (var provider in merged)
            {
                if (provider is IResourceDictionary dict)
                {
                    if (dict.TryGetResource("LocalizationLanguage", null, out var langVal) && langVal is string langStr)
                    {
                        if (langStr == "en")
                        {
                            _enUsDictionary = dict;
                        }
                        else if (langStr == "zh")
                        {
                            _zhCnDictionary = dict;
                        }
                    }
                }
            }
        }

        // Remove both from MergedDictionaries to ensure a clean state
        if (_enUsDictionary is not null) merged.Remove(_enUsDictionary);
        if (_zhCnDictionary is not null) merged.Remove(_zhCnDictionary);

        // Add the correct target dictionary
        var target = lang == "zh" ? _zhCnDictionary : _enUsDictionary;
        if (target is not null)
        {
            merged.Add(target);
        }

        // Refresh filter option display names
        foreach (var opt in FilterOptions)
        {
            if (FilterKeyToResourceKey.TryGetValue(opt.Key, out var resKey))
            {
                opt.DisplayName = L(resKey, opt.Key);
            }
        }

        // Refresh computed text properties
        OnPropertyChanged(nameof(SessionCountText));
        OnPropertyChanged(nameof(EventCountText));
        OnPropertyChanged(nameof(SelectedSessionPath));
        OnPropertyChanged(nameof(SyncNavigationShortcutText));
        OnPropertyChanged(nameof(SyncShortcutEditorText));
    }

    /// <summary>Look up a localized string resource by key, with a fallback default.</summary>
    internal string L(string key, string fallback)
    {
        if (Application.Current?.Resources.TryGetResource(key, null, out var val) == true && val is string s)
        {
            return s;
        }
        return fallback;
    }

    /// <summary>Look up a localized format-string resource by key, then apply string.Format.</summary>
    internal string LF(string key, string fallbackFormat, params object[] args)
    {
        var fmt = L(key, fallbackFormat);
        return string.Format(fmt, args);
    }

    private static ShortcutGesture? ParseShortcutText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var parts = value.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            return null;
        }

        var ctrl = parts.Any(x => string.Equals(x, "Ctrl", StringComparison.OrdinalIgnoreCase));
        var shift = parts.Any(x => string.Equals(x, "Shift", StringComparison.OrdinalIgnoreCase));
        var alt = parts.Any(x => string.Equals(x, "Alt", StringComparison.OrdinalIgnoreCase));
        var keyText = parts.LastOrDefault(x => !ShortcutGesture.IsModifierText(x)) ?? string.Empty;
        var gesture = ShortcutGesture.Create(ctrl, shift, alt, keyText);
        return gesture.IsValid ? gesture : null;
    }

    private static int GetAnchorIndex(IReadOnlyList<DisplayEvent> events, DisplayEvent? anchor, int direction)
    {
        if (anchor is not null)
        {
            var exactIndex = events
                .Select((evt, index) => new { evt, index })
                .FirstOrDefault(x => string.Equals(x.evt.Id, anchor.Id, StringComparison.Ordinal));

            if (exactIndex is not null)
            {
                return exactIndex.index;
            }

            var anchorKey = EventSortTimestamp(anchor);
            var nearbyIndex = direction > 0
                ? FindLastIndex(events, x => EventSortTimestamp(x) <= anchorKey)
                : FindFirstIndex(events, x => EventSortTimestamp(x) >= anchorKey);

            if (nearbyIndex >= 0)
            {
                return nearbyIndex;
            }
        }

        return direction > 0 ? -1 : events.Count;
    }

    private DisplayEvent? GetCompanionEvent(DisplayEvent target)
    {
        var companionPane = target.Pane == EventPane.Conversation
            ? EventPane.Execution
            : EventPane.Conversation;

        var companionEvents = companionPane == EventPane.Conversation
            ? ConversationEvents
            : ExecutionEvents;

        var targetKey = EventSortTimestamp(target);
        return companionEvents
            .Where(x => EventSortTimestamp(x) <= targetKey)
            .OrderByDescending(EventSortTimestamp)
            .ThenByDescending(x => x.LineNumber)
            .FirstOrDefault();
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

    private async Task StartBackgroundEnrichmentAsync(CancellationToken ct)
    {
        var snapshot = Sessions.Where(s => !s.IsEnriched).ToList();
        if (snapshot.Count == 0)
        {
            return;
        }

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 4,
            CancellationToken = ct
        };

        try
        {
            await Parallel.ForEachAsync(snapshot, options, async (session, token) =>
            {
                var summary = await Task.Run(() => _scanner.TryGetSessionSummary(session.FilePath), token);
                if (summary is not null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => session.CopySummaryFrom(summary));
                }
            });
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task EnrichSingleSessionAsync(SessionInfo session)
    {
        try
        {
            var summary = await Task.Run(() => _scanner.TryGetSessionSummary(session.FilePath));
            if (summary is not null)
            {
                await Dispatcher.UIThread.InvokeAsync(() => session.CopySummaryFrom(summary));
            }
        }
        catch (Exception)
        {
            // Ignore enrichment failures for individual sessions
        }
    }

    public void Dispose()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _enrichCts?.Cancel();
        _enrichCts?.Dispose();
        _sessionFilterCts?.Cancel();
        _sessionFilterCts?.Dispose();
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
