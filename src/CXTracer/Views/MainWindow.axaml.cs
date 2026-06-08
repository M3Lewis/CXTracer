using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SukiUI.Controls;
using SukiUI.Toasts;
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
            string description = evt.IsRaw ? "Raw JSON" : "Event text";
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
        var vsp = FindVirtualizingStackPanel(listBox);
        var containers = GetItemContainers(listBox, vsp);

        if (containers.Count == 0)
        {
            return null;
        }

        const double topTolerance = 12;
        ContentPresenter? candidate = null;

        foreach (var container in containers)
        {
            var top = GetContainerExtentTop(container, vsp);
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

    private void ScrollEventIntoView(DisplayEvent evt)
    {
        var (listBox, scrollViewer) = ControlsForPane(evt.Pane);
        var requestVersion = IncrementScrollRequestVersion(evt.Pane);

        if (scrollViewer is null)
        {
            return;
        }

        var vsp = FindVirtualizingStackPanel(listBox);

        // 如果容器已在视觉树中，直接精确对齐
        if (TryScrollEventContainerToTop(listBox, scrollViewer, vsp, evt))
        {
            QueueScrollEventAlignment(evt, requestVersion, remainingAttempts: 3);
            return;
        }

        // 容器未虚拟化：用 ScrollIntoView 触发虚拟化，使容器进入视觉树
        // ScrollIntoView 只保证"最小移动使其可见"，不保证顶部对齐，
        // 因此后续由 VerifyScrollEventAlignment 做精确对齐
        var itemIndex = FindEventIndex(listBox, evt);
        if (itemIndex >= 0)
        {
            listBox.ScrollIntoView(itemIndex);
        }

        QueueScrollEventAlignment(evt, requestVersion, remainingAttempts: 10);
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
            return;
        }

        var (listBox, scrollViewer) = ControlsForPane(evt.Pane);
        if (scrollViewer is null)
        {
            return;
        }

        listBox.UpdateLayout();
        var vsp = FindVirtualizingStackPanel(listBox);

        if (IsEventVisibleAndAligned(listBox, scrollViewer, vsp, evt))
        {
            return;
        }

        if (TryScrollEventContainerToTop(listBox, scrollViewer, vsp, evt))
        {
            // 对齐成功，再验证一次确认稳定
            if (remainingAttempts > 0)
            {
                QueueScrollEventAlignment(evt, requestVersion, remainingAttempts: 1);
            }
            return;
        }

        // 容器仍未虚拟化，等待下次重试，不做任何估算
        if (remainingAttempts > 0)
        {
            QueueScrollEventAlignment(evt, requestVersion, remainingAttempts - 1);
        }
    }

    private static bool TryScrollEventContainerToTop(
        ListBox listBox,
        ScrollViewer scrollViewer,
        VirtualizingStackPanel? vsp,
        DisplayEvent evt)
    {
        if (!TryGetEventContainer(listBox, evt, out var target))
        {
            return false;
        }

        return TryScrollContainerToTop(scrollViewer, target, vsp);
    }

    private static bool IsEventVisibleAndAligned(
        ListBox listBox,
        ScrollViewer scrollViewer,
        VirtualizingStackPanel? vsp,
        DisplayEvent evt)
    {
        return TryGetEventContainer(listBox, evt, out var target)
            && IsContainerVisible(scrollViewer, target, vsp)
            && IsContainerAtDesiredScrollOffset(scrollViewer, target, vsp);
    }

    private static bool TryGetEventContainer(
        ListBox listBox,
        DisplayEvent evt,
        out ContentPresenter target)
    {
        var vsp = FindVirtualizingStackPanel(listBox);
        var match = GetItemContainers(listBox, vsp)
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

    // VirtualizingStackPanel 是 extent 坐标系的原点。
    // TranslatePoint 到 VSP 得到的 Y 值与 ScrollViewer.Offset.Y 处于同一坐标系，
    // 且不随 Offset 变化——这是与 ScrollContentPresenter 方案的关键区别。
    private static VirtualizingStackPanel? FindVirtualizingStackPanel(Control listBox)
    {
        return listBox.GetVisualDescendants()
            .OfType<VirtualizingStackPanel>()
            .FirstOrDefault();
    }

    private static List<ContentPresenter> GetItemContainers(
        ListBox listBox,
        VirtualizingStackPanel? vsp)
    {
        return listBox
            .GetVisualDescendants()
            .OfType<ContentPresenter>()
            .Where(x => x.IsVisible && x.Bounds.Height > 0 && x.DataContext is DisplayEvent)
            .OrderBy(x => GetContainerExtentTop(x, vsp) ?? double.MaxValue)
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
        ContentPresenter target,
        VirtualizingStackPanel? vsp)
    {
        if (!TryGetDesiredScrollOffset(scrollViewer, target, vsp, out var desiredY))
        {
            target.BringIntoView();
            return false;
        }

        scrollViewer.Offset = new Vector(0, desiredY);
        return true;
    }

    private static bool IsContainerVisible(
        ScrollViewer scrollViewer,
        ContentPresenter target,
        VirtualizingStackPanel? vsp)
    {
        if (!TryGetContainerExtentBounds(target, vsp, out var top, out var bottom)
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
        ContentPresenter target,
        VirtualizingStackPanel? vsp)
    {
        if (!TryGetDesiredScrollOffset(scrollViewer, target, vsp, out var desiredY))
        {
            return false;
        }

        const double offsetTolerance = 1;
        return Math.Abs(scrollViewer.Offset.Y - desiredY) <= offsetTolerance;
    }

    private static bool TryGetDesiredScrollOffset(
        ScrollViewer scrollViewer,
        ContentPresenter target,
        VirtualizingStackPanel? vsp,
        out double desiredY)
    {
        if (!TryGetContainerExtentBounds(target, vsp, out var top, out _))
        {
            desiredY = 0;
            return false;
        }

        const double topPadding = 8;
        var maxY = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
        desiredY = Math.Clamp(top - topPadding, 0, maxY);
        return true;
    }

    /// <summary>
    /// 用 TranslatePoint 把 ListBoxItem 的左上角变换到 VirtualizingStackPanel 坐标系。
    ///
    /// 为什么选 VSP 而不是 ScrollContentPresenter：
    ///   - ScrollContentPresenter 的坐标随 Offset 偏移，TranslatePoint 到它得到视口坐标，
    ///     需要再加 Offset.Y 才能换算成 extent 坐标。但"加 Offset.Y"这一步在两个不同帧
    ///     之间会因 Offset 已变而算出不同结果，导致校正时闪烁。
    ///   - VirtualizingStackPanel 本身不随滚动移动，它的坐标系就是 extent 坐标系，
    ///     TranslatePoint 到它得到的 Y 值直接等于 extent top，无需任何补偿，跨帧稳定。
    /// </summary>
    private static bool TryGetContainerExtentBounds(
        ContentPresenter target,
        VirtualizingStackPanel? vsp,
        out double top,
        out double bottom)
    {
        var item = target.GetVisualAncestors().OfType<ListBoxItem>().FirstOrDefault();
        if (item is null || item.Bounds.Height <= 0)
        {
            top = 0;
            bottom = 0;
            return false;
        }

        if (vsp is not null)
        {
            var pt = item.TranslatePoint(new Point(0, 0), vsp);
            if (pt.HasValue)
            {
                top = pt.Value.Y;
                bottom = top + item.Bounds.Height;
                return true;
            }
        }

        // 回退：VSP 未找到时用 Bounds.Y（比 ScrollContentPresenter 方案更稳定，
        // 因为 Bounds.Y 相对于直接父节点，在 VSP 就是直接父节点的情况下等价）
        top = item.Bounds.Y;
        bottom = top + item.Bounds.Height;
        return true;
    }

    private static double? GetContainerExtentTop(
        ContentPresenter target,
        VirtualizingStackPanel? vsp)
    {
        return TryGetContainerExtentBounds(target, vsp, out var top, out _)
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
