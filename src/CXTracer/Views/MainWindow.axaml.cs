using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using SukiUI.Toasts;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SukiUI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CXTracer.Models;
using CXTracer.ViewModels;

namespace CXTracer.Views;

public partial class MainWindow : SukiWindow
{
    private SettingsWindow? _settingsWindow;
    private ScrollViewer? _conversationScrollViewer;
    private ScrollViewer? _executionScrollViewer;
    private int _conversationScrollRequestVersion;
    private int _executionScrollRequestVersion;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void ConversationUp_Click(object? sender, RoutedEventArgs e)
    {
        NavigatePane(EventPane.Conversation, -1);
    }

    private void ConversationDown_Click(object? sender, RoutedEventArgs e)
    {
        NavigatePane(EventPane.Conversation, 1);
    }

    private void ExecutionUp_Click(object? sender, RoutedEventArgs e)
    {
        NavigatePane(EventPane.Execution, -1);
    }

    private void ExecutionDown_Click(object? sender, RoutedEventArgs e)
    {
        NavigatePane(EventPane.Execution, 1);
    }

    private void Settings_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            if (_settingsWindow is { IsVisible: true })
            {
                _settingsWindow.Activate();
                return;
            }

            var settingsVm = new SettingsWindowViewModel(viewModel);
            _settingsWindow = new SettingsWindow
            {
                DataContext = settingsVm
            };
            _settingsWindow.Closed += (_, _) =>
            {
                _settingsWindow = null;
                settingsVm.Dispose();
            };
            _settingsWindow.Show(this);
        }
    }

    private void CardBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel
            || sender is not Border { DataContext: DisplayEvent evt })
        {
            return;
        }

        var props = e.GetCurrentPoint(null).Properties;
        if (props.IsLeftButtonPressed)
        {
            viewModel.ShowDetailPopup(evt);
        }
        else if (props.IsRightButtonPressed)
        {
            string copyText = evt.IsRaw ? evt.RawJson : evt.Text;
            _ = CopyToClipboardAsync(copyText, viewModel);
            e.Handled = true;
        }
    }

    private void DetailPanel_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel
            && viewModel.DetailPopupEvent is { } evt
            && e.GetCurrentPoint(null).Properties.IsRightButtonPressed)
        {
            _ = CopyToClipboardAsync(evt.Text, viewModel);
            e.Handled = true;
        }
    }

    private void SessionPath_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel
            && !string.IsNullOrEmpty(viewModel.SelectedSessionPath)
            && e.GetCurrentPoint(null).Properties.IsRightButtonPressed)
        {
            _ = CopyToClipboardAsync(viewModel.SelectedSessionPath, viewModel);
            e.Handled = true;
        }
    }

    private async Task CopyToClipboardAsync(string text, MainWindowViewModel viewModel)
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(text);

            viewModel.ToastManager.CreateToast()
                .WithTitle(viewModel.L("ToastCopied", "Copied"))
                .WithContent(viewModel.L("ToastCopiedContent", "Copied to clipboard."))
                .Dismiss().After(TimeSpan.FromSeconds(2))
                .Queue();
        }
    }

    private void DetailBackdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.CloseDetailPopup();
        }
    }

    private void DetailClose_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.CloseDetailPopup();
        }
    }

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        var alt = e.KeyModifiers.HasFlag(KeyModifiers.Alt);

        if (ShortcutCaptureInput.TryHandleCapture(
            e,
            viewModel.IsCapturingSyncShortcut,
            viewModel.CaptureSyncShortcut,
            viewModel.RejectSyncShortcutCapture))
        {
            return;
        }

        if (IsTextInputFocused(e.Source))
        {
            return;
        }

        var keyText = ShortcutKeyInput.ToShortcutKeyText(e.Key);
        if (keyText.Length > 0 && viewModel.MatchesSyncNavigationShortcut(ctrl, shift, alt, keyText))
        {
            viewModel.ToggleSynchronizedNavigation();
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.Escape when viewModel.IsDetailPopupOpen:
                viewModel.CloseDetailPopup();
                e.Handled = true;
                return;
            case Key.Left when viewModel.IsDetailPopupOpen:
            case Key.Right when viewModel.IsDetailPopupOpen:
            case Key.Up when viewModel.IsDetailPopupOpen:
            case Key.Down when viewModel.IsDetailPopupOpen:
                e.Handled = true;
                return;
            case Key.Left:
                viewModel.SetActiveTranscriptPane(EventPane.Conversation);
                e.Handled = true;
                break;
            case Key.Right:
                viewModel.SetActiveTranscriptPane(EventPane.Execution);
                e.Handled = true;
                break;
            case Key.Up:
                NavigateActivePane(viewModel, -1);
                e.Handled = true;
                break;
            case Key.Down:
                NavigateActivePane(viewModel, 1);
                e.Handled = true;
                break;
        }
    }

    private void NavigateActivePane(MainWindowViewModel viewModel, int direction)
    {
        var pane = viewModel.ActiveTranscriptPane == EventPane.Execution
            ? EventPane.Execution
            : EventPane.Conversation;

        NavigatePane(pane, direction);
    }

    private void NavigatePane(EventPane pane, int direction)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var effectivePane = viewModel.IsSynchronizedNavigationEnabled
            && viewModel.CurrentTranscriptEvent is not null
            ? viewModel.ActiveTranscriptPane
            : pane;

        viewModel.SetActiveTranscriptPane(effectivePane);
        var (listBox, scrollViewer) = ControlsForPane(effectivePane);

        if (scrollViewer is null)
        {
            return;
        }

        if (!viewModel.IsSynchronizedNavigationEnabled)
        {
            var target = FindAdjacentMessage(listBox, scrollViewer, direction);
            viewModel.SetCurrentTranscriptEvent(target);
            if (target is not null)
            {
                ScrollEventIntoView(target);
            }
            return;
        }

        var anchor = GetAnchorEvent(listBox, scrollViewer);
        var navigationTarget = viewModel.GetSynchronizedNavigationTarget(effectivePane, direction, anchor);
        ScrollNavigationTarget(navigationTarget);
    }

    private void ScrollNavigationTarget(TranscriptNavigationTarget? navigationTarget)
    {
        if (navigationTarget is null)
        {
            return;
        }

        if (navigationTarget.Companion is not null)
        {
            ScrollEventIntoView(navigationTarget.Companion);
        }

        ScrollEventIntoView(navigationTarget.Target);
    }

    private DisplayEvent? FindAdjacentMessage(
        ListBox listBox,
        ScrollViewer scrollViewer,
        int direction)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return null;
        }

        var events = listBox.ItemsSource?.Cast<DisplayEvent>().ToList();
        if (events == null || events.Count == 0)
        {
            return null;
        }

        var currentPane = listBox == ConversationListBox ? EventPane.Conversation : EventPane.Execution;
        var anchor = viewModel.CurrentTranscriptEvent is not null
            && viewModel.CurrentTranscriptEvent.Pane == currentPane
            && events.Contains(viewModel.CurrentTranscriptEvent)
            ? viewModel.CurrentTranscriptEvent
            : GetAnchorEvent(listBox, scrollViewer);

        int anchorIndex = anchor != null ? events.IndexOf(anchor) : -1;
        if (anchorIndex == -1)
        {
            anchorIndex = direction > 0 ? -1 : events.Count;
        }

        int targetIndex = Math.Clamp(anchorIndex + Math.Sign(direction), 0, events.Count - 1);
        return events[targetIndex];
    }

    private DisplayEvent? GetAnchorEvent(ListBox listBox, ScrollViewer scrollViewer)
    {
        var containers = GetItemContainers(listBox);

        if (containers.Count == 0)
        {
            return null;
        }

        const double topTolerance = 12;
        ContentPresenter? candidate = null;

        foreach (var container in containers)
        {
            var top = GetContainerExtentTop(container, scrollViewer);
            if (top is null)
            {
                continue;
            }

            if (top.Value <= scrollViewer.Offset.Y + topTolerance)
            {
                candidate = container;
                continue;
            }

            break;
        }

        candidate ??= containers[0];
        return candidate.DataContext as DisplayEvent;
    }

    private static void Log(string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    }

    private void ScrollEventIntoView(DisplayEvent evt)
    {
        Log("==================== SCROLL START ====================");
        var (listBox, scrollViewer) = ControlsForPane(evt.Pane);
        var requestVersion = IncrementScrollRequestVersion(evt.Pane);
        Log($"[ScrollEventIntoView] Entry. Pane: {evt.Pane}, Event: {evt.Id}, ReqVersion: {requestVersion}");

        if (scrollViewer is null)
        {
            Log($"[ScrollEventIntoView] Abort: ScrollViewer is null. Pane: {evt.Pane}");
            Log("==================== SCROLL END (Viewer Null) ====================");
            return;
        }

        if (TryScrollEventContainerToTop(listBox, scrollViewer, evt))
        {
            Log($"[ScrollEventIntoView] SUCCESS: TryScrollEventContainerToTop was successful immediately. Queuing 3 alignments for Event: {evt.Id}");
            QueueScrollEventAlignment(evt, requestVersion, remainingAttempts: 3);
            return;
        }

        var estimatedOffset = EstimateExtentOffset(listBox, scrollViewer, evt);
        Log($"[ScrollEventIntoView] Immediate alignment failed. EstimatedOffset Y: {estimatedOffset}");
        if (estimatedOffset.HasValue)
        {
            Log($"[ScrollEventIntoView] Setting estimated offset.Y to {estimatedOffset.Value}");
            scrollViewer.Offset = new Vector(0, estimatedOffset.Value);
        }

        QueueScrollEventAlignment(evt, requestVersion, remainingAttempts: 10);
    }

    private static double? EstimateExtentOffset(
        ListBox listBox,
        ScrollViewer scrollViewer,
        DisplayEvent evt)
    {
        var allItems = listBox.Items.Cast<object>().ToList();
        var targetIndex = allItems.FindIndex(x =>
            x is DisplayEvent de && string.Equals(de.Id, evt.Id, StringComparison.Ordinal));

        if (targetIndex < 0)
        {
            Log($"[EstimateExtentOffset] Target Index not found in Items Source for Event: {evt.Id}");
            return null;
        }

        var totalItems = allItems.Count;
        if (totalItems == 0)
        {
            Log("[EstimateExtentOffset] Total Items is 0");
            return null;
        }

        var extentHeight = scrollViewer.Extent.Height;
        if (extentHeight <= 0)
        {
            Log($"[EstimateExtentOffset] scrollViewer.Extent.Height is <= 0 ({extentHeight})");
            return null;
        }

        var avgItemHeight = extentHeight / totalItems;

        const double topPadding = 8;
        var estimatedTop = targetIndex * avgItemHeight;
        var maxOffset = Math.Max(0, extentHeight - scrollViewer.Viewport.Height);
        var result = Math.Clamp(estimatedTop - topPadding, 0, maxOffset);
        Log($"[EstimateExtentOffset] Event: {evt.Id}, targetIndex: {targetIndex}, totalItems: {totalItems}, extentHeight: {extentHeight}, avgItemHeight: {avgItemHeight}, estimatedTop: {estimatedTop}, maxOffset: {maxOffset}, result: {result}");
        return result;
    }

    private static int FindEventIndex(ListBox listBox, DisplayEvent evt)
    {
        return listBox.Items.Cast<object>().ToList().FindIndex(x =>
            x is DisplayEvent de && string.Equals(de.Id, evt.Id, StringComparison.Ordinal));
    }

    private int IncrementScrollRequestVersion(EventPane pane)
    {
        return pane == EventPane.Execution
            ? ++_executionScrollRequestVersion
            : ++_conversationScrollRequestVersion;
    }

    private bool IsCurrentScrollRequest(EventPane pane, int requestVersion)
    {
        return pane == EventPane.Execution
            ? _executionScrollRequestVersion == requestVersion
            : _conversationScrollRequestVersion == requestVersion;
    }

    private void QueueScrollEventAlignment(
        DisplayEvent evt,
        int requestVersion,
        int remainingAttempts)
    {
        Dispatcher.UIThread.Post(
            () => VerifyScrollEventAlignment(evt, requestVersion, remainingAttempts),
            DispatcherPriority.Background);
    }

    private void VerifyScrollEventAlignment(
        DisplayEvent evt,
        int requestVersion,
        int remainingAttempts)
    {
        if (!IsCurrentScrollRequest(evt.Pane, requestVersion))
        {
            Log($"[VerifyScrollEventAlignment] Discarded (stale version). Pane: {evt.Pane}, Event: {evt.Id}, RequestVersion: {requestVersion}");
            return;
        }

        var (listBox, scrollViewer) = ControlsForPane(evt.Pane);
        if (scrollViewer is null)
        {
            Log($"[VerifyScrollEventAlignment] ScrollViewer is null. Pane: {evt.Pane}");
            Log("==================== SCROLL END (Viewer Null) ====================");
            return;
        }

        listBox.UpdateLayout();

        bool isAligned = IsEventVisibleAndAligned(listBox, scrollViewer, evt);
        Log($"[VerifyScrollEventAlignment] Entry. Event: {evt.Id}, RemainingAttempts: {remainingAttempts}, IsAligned: {isAligned}");
        if (isAligned)
        {
            Log($"[VerifyScrollEventAlignment] ALIGNED SUCCESS. Exiting loop for Event: {evt.Id}");
            Log("==================== SCROLL END ====================");
            return;
        }

        if (TryScrollEventContainerToTop(listBox, scrollViewer, evt))
        {
            Log($"[VerifyScrollEventAlignment] TryScrollEventContainerToTop SUCCESS for Event: {evt.Id}. Queuing 1 final alignment verify.");
            if (remainingAttempts > 0)
            {
                QueueScrollEventAlignment(evt, requestVersion, remainingAttempts: 1);
            }
            return;
        }

        if (remainingAttempts > 0)
        {
            Log($"[VerifyScrollEventAlignment] TryScrollEventContainerToTop failed, calling RefineOffsetFromRealizedContainers...");
            RefineOffsetFromRealizedContainers(listBox, scrollViewer, evt);
            QueueScrollEventAlignment(evt, requestVersion, remainingAttempts - 1);
        }
        else
        {
            Log($"[VerifyScrollEventAlignment] Attempts exhausted! Event {evt.Id} is still not aligned.");
            Log("==================== SCROLL END (Attempts Exhausted) ====================");
        }
    }

    private static void RefineOffsetFromRealizedContainers(
        ListBox listBox,
        ScrollViewer scrollViewer,
        DisplayEvent evt)
    {
        var allItems = listBox.Items.Cast<object>().ToList();
        var targetIndex = allItems.FindIndex(x =>
            x is DisplayEvent de && string.Equals(de.Id, evt.Id, StringComparison.Ordinal));

        if (targetIndex < 0)
        {
            Log($"[RefineOffsetFromRealizedContainers] Target Event {evt.Id} not found in Items Source.");
            return;
        }

        var containers = GetItemContainers(listBox);
        Log($"[RefineOffsetFromRealizedContainers] Event: {evt.Id}, Realized containers count: {containers.Count}");
        if (containers.Count == 0)
        {
            return;
        }

        ContentPresenter? nearest = null;
        int nearestIndex = -1;
        int minDist = int.MaxValue;

        foreach (var container in containers)
        {
            if (container.DataContext is not DisplayEvent de)
            {
                continue;
            }

            var idx = allItems.FindIndex(x =>
                x is DisplayEvent d && string.Equals(d.Id, de.Id, StringComparison.Ordinal));

            if (idx < 0)
            {
                continue;
            }

            int dist = Math.Abs(idx - targetIndex);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = container;
                nearestIndex = idx;
            }
        }

        if (nearest is null || nearestIndex < 0)
        {
            Log($"[RefineOffsetFromRealizedContainers] No valid nearest realized container found for Event: {evt.Id}");
            return;
        }

        var nearestEvt = nearest.DataContext as DisplayEvent;
        bool gotBounds = TryGetContainerExtentBounds(nearest, scrollViewer, out var nearestTop, out var nearestBottom);
        Log($"[RefineOffsetFromRealizedContainers] Nearest container is Event: {nearestEvt?.Id} at Index: {nearestIndex} (Dist: {minDist}). GotBounds: {gotBounds}, Top: {nearestTop}, Bottom: {nearestBottom}");

        if (!gotBounds)
        {
            return;
        }

        var itemHeight = nearestBottom - nearestTop;
        if (itemHeight <= 0)
        {
            Log($"[RefineOffsetFromRealizedContainers] Abort: itemHeight is <= 0 ({itemHeight})");
            return;
        }

        var indexDiff = targetIndex - nearestIndex;
        var estimatedTop = nearestTop + indexDiff * itemHeight;

        const double topPadding = 8;
        var maxOffset = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
        var newOffset = Math.Clamp(estimatedTop - topPadding, 0, maxOffset);
        Log($"[RefineOffsetFromRealizedContainers] indexDiff: {indexDiff}, estimatedTop: {estimatedTop}, newOffset Y: {newOffset}, current offset Y: {scrollViewer.Offset.Y}");
        scrollViewer.Offset = new Vector(0, newOffset);
    }

    private static bool TryScrollEventContainerToTop(
        ListBox listBox,
        ScrollViewer scrollViewer,
        DisplayEvent evt)
    {
        if (!TryGetEventContainer(listBox, evt, out var target))
        {
            return false;
        }

        return TryScrollContainerToTop(scrollViewer, target);
    }

    private static bool IsEventVisibleAndAligned(
        ListBox listBox,
        ScrollViewer scrollViewer,
        DisplayEvent evt)
    {
        return TryGetEventContainer(listBox, evt, out var target)
            && IsContainerVisible(scrollViewer, target)
            && IsContainerAtDesiredScrollOffset(scrollViewer, target);
    }

    private static bool TryGetEventContainer(
        ListBox listBox,
        DisplayEvent evt,
        out ContentPresenter target)
    {
        var match = GetItemContainers(listBox)
            .FirstOrDefault(x => IsEventContainer(x, evt));

        if (match is null)
        {
            target = null!;
            return false;
        }

        target = match;
        return true;
    }

    private (ListBox ListBox, ScrollViewer? ScrollViewer) ControlsForPane(EventPane pane)
    {
        if (pane == EventPane.Execution)
        {
            _executionScrollViewer ??= FindScrollViewer(ExecutionListBox);
            return (ExecutionListBox, _executionScrollViewer);
        }

        _conversationScrollViewer ??= FindScrollViewer(ConversationListBox);
        return (ConversationListBox, _conversationScrollViewer);
    }

    private static ScrollViewer? FindScrollViewer(Control control)
    {
        return control.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
    }

    private static List<ContentPresenter> GetItemContainers(ListBox listBox)
    {
        return listBox
            .GetVisualDescendants()
            .OfType<ContentPresenter>()
            .Where(x =>
                x.IsVisible &&
                x.Bounds.Height > 0 &&
                x.DataContext is DisplayEvent)
            .OrderBy(x => x.Bounds.Y)
            .ToList();
    }

    private static bool IsEventContainer(ContentPresenter container, DisplayEvent evt)
    {
        return ReferenceEquals(container.DataContext, evt)
            || container.DataContext is DisplayEvent candidate
            && string.Equals(candidate.Id, evt.Id, StringComparison.Ordinal);
    }

    private static bool TryScrollContainerToTop(
        ScrollViewer scrollViewer,
        ContentPresenter target)
    {
        bool hasOffset = TryGetDesiredScrollOffset(scrollViewer, target, out var clampedY);
        var evt = target.DataContext as DisplayEvent;
        Log($"[TryScrollContainerToTop] Event: {evt?.Id}, HasDesiredOffset: {hasOffset}, DesiredY: {clampedY}, CurrentOffset.Y: {scrollViewer.Offset.Y}");

        if (!hasOffset)
        {
            Log($"[TryScrollContainerToTop] Desired Y failed, invoking target.BringIntoView() as fallback for Event: {evt?.Id}");
            target.BringIntoView();
            return false;
        }

        Log($"[TryScrollContainerToTop] Aligning scrollViewer.Offset to Y: {clampedY} for Event: {evt?.Id}");
        scrollViewer.Offset = new Vector(0, clampedY);
        return true;
    }

    private static bool IsContainerVisible(
        ScrollViewer scrollViewer,
        ContentPresenter target)
    {
        if (!TryGetContainerExtentBounds(target, scrollViewer, out var top, out var bottom)
            || scrollViewer.Viewport.Height <= 0)
        {
            return false;
        }

        const double visibilityTolerance = 1;
        var viewportTop = scrollViewer.Offset.Y;
        var viewportBottom = viewportTop + scrollViewer.Viewport.Height;
        return bottom > viewportTop + visibilityTolerance
            && top < viewportBottom - visibilityTolerance;
    }

    private static bool IsContainerAtDesiredScrollOffset(
        ScrollViewer scrollViewer,
        ContentPresenter target)
    {
        if (!TryGetDesiredScrollOffset(scrollViewer, target, out var desiredY))
        {
            return false;
        }

        const double offsetTolerance = 1;
        return Math.Abs(scrollViewer.Offset.Y - desiredY) <= offsetTolerance;
    }

    private static bool TryGetDesiredScrollOffset(
        ScrollViewer scrollViewer,
        ContentPresenter target,
        out double desiredY)
    {
        if (!TryGetContainerExtentBounds(target, scrollViewer, out var top, out _))
        {
            desiredY = 0;
            return false;
        }

        const double topPadding = 8;
        var maxY = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
        desiredY = Math.Clamp(top - topPadding, 0, maxY);
        return true;
    }

    private static bool TryGetContainerExtentBounds(
        ContentPresenter target,
        ScrollViewer scrollViewer,
        out double top,
        out double bottom)
    {
        var evt = target.DataContext as DisplayEvent;
        var item = target.GetVisualAncestors().OfType<ListBoxItem>().FirstOrDefault();
        if (item is null)
        {
            Log($"[TryGetContainerExtentBounds] ListBoxItem not found for Event: {evt?.Id}");
            top = 0;
            bottom = 0;
            return false;
        }
        if (item.Bounds.Height <= 0)
        {
            Log($"[TryGetContainerExtentBounds] ListBoxItem bounds height is 0 (unmeasured) for Event: {evt?.Id}");
            top = 0;
            bottom = 0;
            return false;
        }

        var scrollContent = scrollViewer
            .GetVisualDescendants()
            .OfType<ScrollContentPresenter>()
            .FirstOrDefault();

        if (scrollContent is not null)
        {
            var pt = item.TranslatePoint(new Point(0, 0), scrollContent);
            if (pt.HasValue)
            {
                top = pt.Value.Y + scrollViewer.Offset.Y;
                bottom = top + item.Bounds.Height;
                Log($"[TryGetContainerExtentBounds] TranslatePoint SUCCESS. Event: {evt?.Id}, Top: {top}, Bottom: {bottom}, Translate Y: {pt.Value.Y}, ScrollViewer Offset.Y: {scrollViewer.Offset.Y}, ItemHeight: {item.Bounds.Height}");
                return true;
            }
            else
            {
                Log($"[TryGetContainerExtentBounds] TranslatePoint returned null for Event: {evt?.Id}");
            }
        }
        else
        {
            Log($"[TryGetContainerExtentBounds] ScrollContentPresenter is null for Event: {evt?.Id}");
        }

        // 回退
        top = item.Bounds.Y;
        bottom = top + item.Bounds.Height;
        Log($"[TryGetContainerExtentBounds] Fallback to Bounds.Y. Event: {evt?.Id}, Top: {top}, Bottom: {bottom}, Bounds.Y: {item.Bounds.Y}, ItemHeight: {item.Bounds.Height}");
        return true;
    }

    private static double? GetContainerExtentTop(ContentPresenter target, ScrollViewer scrollViewer)
    {
        return TryGetContainerExtentBounds(target, scrollViewer, out var top, out _)
            ? top
            : null;
    }

    private static bool IsTextInputFocused(object? source)
    {
        return source is TextBox
            || source is Visual visual
            && visual.GetVisualAncestors().OfType<TextBox>().Any();
    }
}
