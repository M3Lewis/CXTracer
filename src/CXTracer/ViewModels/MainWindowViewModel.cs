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

    private const int SessionBatchSize = 40;
    private const int EventBatchSize = 40;

    private readonly Dictionary<string, CancellationTokenSource> _changeDebouncers = new(StringComparer.OrdinalIgnoreCase);
    private readonly SessionScanner _scanner;
    private readonly SessionReader _reader;
    private readonly SessionWatcher _watcher;
    private readonly AppSettingsService _settingsService;
    private readonly List<DisplayEvent> _allEvents = [];
    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _enrichCts;
    private bool _selectionChanging;
    private bool _isLoadingSettings;
    private ShortcutGesture? _syncNavigationShortcut;

    public ObservableCollection<SessionInfo> Sessions { get; } = [];
    public ObservableCollection<DisplayEvent> ConversationEvents { get; } = [];
    public ObservableCollection<DisplayEvent> ExecutionEvents { get; } = [];
    public ObservableCollection<DisplayEvent> RawEvents { get; } = [];
    public ObservableCollection<string> FilterOptions { get; } = new(new[] { "All", "Conversation", "Commands", "Errors", "Diffs", "Final", "Tools", "Raw" });

    [ObservableProperty]
    private string _rootPath = SessionScanner.DefaultRootPath();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedFilter = "All";

    [ObservableProperty]
    private SessionInfo? _selectedSession;

    [ObservableProperty]
    private string _statusMessage = "Ready";

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

    public string SessionCountText => $"{Sessions.Count} sessions";
    public string EventCountText => $"{VisibleEventCount}/{TotalEventCount} events";
    public string SelectedSessionPath => SelectedSession?.FilePath ?? "No session selected";
    public bool IsConversationPaneActive => ActiveTranscriptPane == EventPane.Conversation;
    public bool IsExecutionPaneActive => ActiveTranscriptPane == EventPane.Execution;
    public string SyncNavigationShortcutText => _syncNavigationShortcut?.DisplayText ?? "Unset";
    public string SyncShortcutEditorText => IsCapturingSyncShortcut
        ? "Press shortcut..."
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
        LoadSettings();
        _ = RefreshAsync();
    }

    partial void OnRootPathChanged(string value)
    {
        StatusMessage = Directory.Exists(value)
            ? "Directory exists. Click Refresh to scan."
            : "Directory does not exist. Check the Codex sessions path.";
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

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedFilterChanged(string value) => ApplyFilter();
    partial void OnShowRawEventsChanged(bool value) => ApplyFilter();
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
            StatusMessage = "Scanning sessions...";
            ResetEvents();
            Sessions.Clear();
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
            await AddSessionsAsync(sessions).ConfigureAwait(true);

            _enrichCts?.Cancel();
            _enrichCts = new CancellationTokenSource();
            _ = StartBackgroundEnrichmentAsync(_enrichCts.Token);

            _watcher.Start(normalized);
            StatusMessage = sessions.Count == 0
                ? "No .jsonl sessions found."
                : $"Loaded {sessions.Count} session entries.";

            if (SelectedSession is null && Sessions.Count > 0)
            {
                _selectionChanging = true;
                SelectedSession = Sessions[0];
                _selectionChanging = false;
                OnPropertyChanged(nameof(SelectedSessionPath));

                await LoadSelectedSessionAsync(Sessions[0]).ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
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
            StatusMessage = "Choose a Ctrl/Shift/Alt + key shortcut first.";
            return;
        }

        _syncNavigationShortcut = gesture;
        PendingSyncShortcutText = string.Empty;
        IsCapturingSyncShortcut = false;
        OnPropertyChanged(nameof(SyncNavigationShortcutText));
        OnPropertyChanged(nameof(SyncShortcutEditorText));
        SaveSettings();
        StatusMessage = $"Sync navigation shortcut set to {gesture.DisplayText}.";
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
        StatusMessage = "Press Ctrl/Shift/Alt + a key for sync navigation.";
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
        StatusMessage = $"Captured {gesture.DisplayText}. Click OK to save.";
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
            ? "Synchronized navigation enabled."
            : "Synchronized navigation disabled.";
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
            StatusMessage = $"Reading {session.FileName}...";
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
            StatusMessage = $"Viewing {session.FileName}";
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            StatusMessage = $"Read failed: {ex.Message}";
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
                    if (target is not null)
                    {
                        SelectedSession = target;
                    }
                }
                else
                {
                    StatusMessage = $"Another session changed: {Path.GetFileName(path)}";
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
            StatusMessage = $"Live update: {events.Count} events";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Live read failed: {ex.Message}";
        }
    }

    private void UpsertSession(string path)
    {
        var info = _scanner.TryGetSession(path);
        if (info is null)
        {
            return;
        }

        var existing = Sessions.FirstOrDefault(s => PathsEqual(s.FilePath, info.FilePath));
        if (existing is null)
        {
            Sessions.Insert(0, info);
            _ = EnrichSingleSessionAsync(info);
        }
        else
        {
            existing.LastWriteTime = info.LastWriteTime;
            existing.Length = info.Length;

            int oldIndex = Sessions.IndexOf(existing);
            int targetIndex = 0;
            while (targetIndex < Sessions.Count && Sessions[targetIndex].LastWriteTime > existing.LastWriteTime)
            {
                targetIndex++;
            }

            if (targetIndex > oldIndex) targetIndex--;

            if (oldIndex != targetIndex)
            {
                _selectionChanging = true;
                var currentSelection = SelectedSession;
                Sessions.RemoveAt(oldIndex);
                Sessions.Insert(targetIndex, existing);
                SelectedSession = currentSelection;
                _selectionChanging = false;
            }
        }

        OnPropertyChanged(nameof(SessionCountText));
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
                if (ShowRawEvents || SelectedFilter == "Raw")
                {
                    RawEvents.Add(evt);
                }
                break;
        }
    }

    private bool PassesFilter(DisplayEvent evt)
    {
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var q = SearchText.Trim();
            if (!evt.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                && !evt.Text.Contains(q, StringComparison.OrdinalIgnoreCase)
                && !evt.RawJson.Contains(q, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return SelectedFilter switch
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
            _isLoadingSettings = false;

            OnPropertyChanged(nameof(SyncNavigationShortcutText));
            OnPropertyChanged(nameof(SyncShortcutEditorText));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            _isLoadingSettings = false;
            StatusMessage = $"Settings load failed: {ex.Message}";
        }
    }

    private void SaveSettings()
    {
        try
        {
            _settingsService.Save(new AppSettings
            {
                IsSynchronizedNavigationEnabled = IsSynchronizedNavigationEnabled,
                SyncNavigationShortcut = _syncNavigationShortcut
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            StatusMessage = $"Settings save failed: {ex.Message}";
        }
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
