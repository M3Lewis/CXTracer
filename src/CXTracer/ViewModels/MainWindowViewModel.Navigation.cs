using System;
using System.Collections.Generic;
using System.Linq;
using CXTracer.Models;

namespace CXTracer.ViewModels;

public sealed partial class MainWindowViewModel
{
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
        if (_sortedSyncEventsCache is null)
        {
            _sortedSyncEventsCache = ConversationEvents
                .Concat(ExecutionEvents)
                .OrderBy(EventSortTimestamp)
                .ThenBy(x => x.LineNumber)
                .ToList();
        }

        var events = _sortedSyncEventsCache;

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
}
