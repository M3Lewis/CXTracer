using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CXTracer.Models;

namespace CXTracer.ViewModels;

public sealed partial class MainWindowViewModel
{
    private async Task ApplyFilterAsync(CancellationToken ct, bool debounce = false)
    {
        try
        {
            if (debounce)
            {
                await Task.Delay(200, ct).ConfigureAwait(true);
            }

            var previousSelected = CurrentTranscriptEvent;
            ConversationEvents.Clear();
            ExecutionEvents.Clear();
            RawEvents.Clear();
            UpdateVisibleEventCount();

            var q = EventSearchText;
            var hasQuery = !string.IsNullOrWhiteSpace(q);
            var qTrimmed = hasQuery ? q.Trim() : null;
            var searchRaw = ShowRawEvents || SelectedFilter?.Key == "Raw";

            var count = 0;
            foreach (var evt in _allEvents)
            {
                ct.ThrowIfCancellationRequested();

                if (PassesFilterInternal(evt, qTrimmed, searchRaw))
                {
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

                count++;

                if (count % EventBatchSize == 0)
                {
                    UpdateVisibleEventCount();
                    await YieldToUiAsync().ConfigureAwait(true);
                }
            }

            UpdateVisibleEventCount();

            if (previousSelected is not null)
            {
                bool isStillVisible = false;
                if (previousSelected.Pane == EventPane.Conversation)
                {
                    isStillVisible = ConversationEvents.Contains(previousSelected);
                }
                else if (previousSelected.Pane == EventPane.Execution)
                {
                    isStillVisible = ExecutionEvents.Contains(previousSelected);
                }

                if (isStillVisible)
                {
                    SetCurrentTranscriptEvent(previousSelected);
                    FilterAppliedScrollRequest?.Invoke(previousSelected);
                }
                else
                {
                    SetCurrentTranscriptEvent(null);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
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

    private void ResetEvents()
    {
        _allEvents.Clear();
        _sortedSyncEventsCache = null;
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
        _sortedSyncEventsCache = null;
        VisibleEventCount = ConversationEvents.Count + ExecutionEvents.Count + RawEvents.Count;
        OnPropertyChanged(nameof(EventCountText));
    }

    private void UpdateEventSequenceNumbers()
    {
        var sorted = _allEvents
            .Where(e => e.Pane is EventPane.Conversation or EventPane.Execution)
            .OrderBy(EventSortTimestamp)
            .ThenBy(e => e.LineNumber)
            .ToList();

        int convCount = 0;
        int execCount = 0;
        for (int i = 0; i < sorted.Count; i++)
        {
            var evt = sorted[i];
            int paneSeq;
            if (evt.Pane == EventPane.Conversation)
            {
                convCount++;
                paneSeq = convCount;
            }
            else
            {
                execCount++;
                paneSeq = execCount;
            }
            evt.ColumnSequenceText = paneSeq.ToString();
            evt.MergedSequenceText = (i + 1).ToString();
        }
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
        var q = EventSearchText;
        if (!string.IsNullOrWhiteSpace(q))
        {
            var qTrimmed = q.Trim();
            var searchRaw = ShowRawEvents || SelectedFilter?.Key == "Raw";
            return PassesFilterInternal(evt, qTrimmed, searchRaw);
        }
        return PassesFilterInternal(evt, null, false);
    }

    private bool PassesFilterInternal(DisplayEvent evt, string? qTrimmed, bool searchRaw)
    {
        if (qTrimmed != null)
        {
            var matchText = evt.Title.Contains(qTrimmed, StringComparison.OrdinalIgnoreCase) || 
                            evt.Text.Contains(qTrimmed, StringComparison.OrdinalIgnoreCase);

            if (!matchText && (!searchRaw || !evt.RawJson.Contains(qTrimmed, StringComparison.OrdinalIgnoreCase)))
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
}
