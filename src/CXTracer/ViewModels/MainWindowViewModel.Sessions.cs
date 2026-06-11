using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CXTracer.Models;

namespace CXTracer.ViewModels;

public sealed partial class MainWindowViewModel
{
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

            foreach (var evt in events)
            {
                evt.ResetExpansionState(ExpandAllEventsByDefault);
            }

            _allEvents.Clear();
            _allEvents.AddRange(events);
            DisplayEvent.ResolvePlaceholderImages(_allEvents);
            UpdateEventSequenceNumbers();
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
                evt.ResetExpansionState(ExpandAllEventsByDefault);
                _allEvents.Add(evt);
            }

            DisplayEvent.ResolvePlaceholderImages(_allEvents);
            UpdateEventSequenceNumbers();

            foreach (var evt in events)
            {
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
}
