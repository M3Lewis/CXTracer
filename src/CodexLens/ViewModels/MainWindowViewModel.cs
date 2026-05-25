using Avalonia.Threading;
using CodexLens.Models;
using CodexLens.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodexLens.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private const int SessionBatchSize = 40;
    private const int EventBatchSize = 40;

    private readonly Dictionary<string, CancellationTokenSource> _changeDebouncers = new(StringComparer.OrdinalIgnoreCase);
    private readonly SessionScanner _scanner;
    private readonly SessionReader _reader;
    private readonly SessionWatcher _watcher;
    private readonly List<DisplayEvent> _allEvents = [];
    private CancellationTokenSource? _loadCts;
    private bool _selectionChanging;

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
    private int _totalEventCount;

    [ObservableProperty]
    private int _visibleEventCount;

    public string SessionCountText => $"{Sessions.Count} sessions";
    public string EventCountText => $"{VisibleEventCount}/{TotalEventCount} events";
    public string SelectedSessionPath => SelectedSession?.FilePath ?? "No session selected";

    public MainWindowViewModel(SessionScanner scanner, SessionReader reader, SessionWatcher watcher)
    {
        _scanner = scanner;
        _reader = reader;
        _watcher = watcher;
        _watcher.SessionFileChanged += OnSessionFileChanged;
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
        }
        else
        {
            existing.LastWriteTime = info.LastWriteTime;
            existing.Length = info.Length;
        }

        var sorted = Sessions.OrderByDescending(s => s.LastWriteTime).ToList();
        _selectionChanging = true;
        Sessions.Clear();
        foreach (var session in sorted)
        {
            Sessions.Add(session);
        }
        _selectionChanging = false;
        OnPropertyChanged(nameof(SessionCountText));
    }

    private void ApplyFilter()
    {
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
