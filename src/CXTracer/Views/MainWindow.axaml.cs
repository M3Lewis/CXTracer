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

    // 核心：同步、无跳动滚动定位
    private void ScrollEventIntoView(DisplayEvent evt)
    {
        var (listBox, scrollViewer) = ControlsForPane(evt.Pane);
        if (scrollViewer is null) return;

        // 1. 尝试直接对齐（容器已在视觉树）
        if (TryGetEventContainer(listBox, evt, out var target))
        {
            ScrollContainerToTop(scrollViewer, target);
            return;
        }

        // 2. 容器未虚拟化：用 ScrollIntoView 触发虚拟化
        var itemIndex = FindEventIndex(listBox, evt);
        if (itemIndex < 0) return;

        listBox.ScrollIntoView(itemIndex);
        listBox.UpdateLayout();

        // 3. 立即获取容器并精确对齐到顶部
        if (TryGetEventContainer(listBox, evt, out target))
        {
            ScrollContainerToTop(scrollViewer, target);
        }
    }

    // 将容器滚动到顶部（保持 8px 间距）
    private static void ScrollContainerToTop(ScrollViewer scrollViewer, ContentPresenter target)
    {
        var vsp = FindVirtualizingStackPanel(target);
        if (TryGetContainerExtentBounds(target, vsp, out var top, out _))
        {
            const double topPadding = 8;
            var maxOffset = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
            var desiredY = Math.Clamp(top - topPadding, 0, maxOffset);
            scrollViewer.Offset = new Vector(0, desiredY);
        }
        else
        {
            target.BringIntoView();
        }
    }

    private static bool TryGetEventContainer(ListBox listBox, DisplayEvent evt, out ContentPresenter target)
    {
        var vsp = FindVirtualizingStackPanel(listBox);
        var match = GetItemContainers(listBox, vsp)
            .FirstOrDefault(x => IsEventContainer(x, evt));

        if (match is not null)
        {
            target = match;
            return true;
        }

        target = null!;
        return false;
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

    private static VirtualizingStackPanel? FindVirtualizingStackPanel(Control control)
    {
        return control.GetVisualDescendants()
            .OfType<VirtualizingStackPanel>()
            .FirstOrDefault();
    }

    private static List<ContentPresenter> GetItemContainers(ListBox listBox, VirtualizingStackPanel? vsp)
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

    private static int FindEventIndex(ListBox listBox, DisplayEvent evt)
    {
        return listBox.Items.Cast<object>().ToList().FindIndex(x =>
            x is DisplayEvent de && string.Equals(de.Id, evt.Id, StringComparison.Ordinal));
    }

    // 使用 VirtualizingStackPanel 坐标系，保证坐标稳定不随滚动偏移
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

        // 回退：极少数情况下 VSP 未找到时使用 Bounds.Y
        top = item.Bounds.Y;
        bottom = top + item.Bounds.Height;
        return true;
    }

    private static double? GetContainerExtentTop(ContentPresenter target, VirtualizingStackPanel? vsp)
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
