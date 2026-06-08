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
using System.ComponentModel;
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
    private MainWindowViewModel? _registeredViewModel;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_registeredViewModel is not null)
        {
            _registeredViewModel.FilterAppliedScrollRequest -= ViewModel_FilterAppliedScrollRequest;
        }

        if (DataContext is MainWindowViewModel viewModel)
        {
            _registeredViewModel = viewModel;
            viewModel.FilterAppliedScrollRequest += ViewModel_FilterAppliedScrollRequest;
        }
        else
        {
            _registeredViewModel = null;
        }
    }

    private void ViewModel_FilterAppliedScrollRequest(DisplayEvent selected)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ScrollEventIntoView(selected);
        }, DispatcherPriority.Background);
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
            viewModel.SetCurrentTranscriptEvent(evt);
            this.Focus();

            if (e.ClickCount == 2)
            {
                viewModel.ShowDetailPopup(evt);
            }
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
            if (e.Key != Key.Up && e.Key != Key.Down)
            {
                return;
            }
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

        if (listBox.ItemsSource is not System.Collections.IList list || list.Count == 0)
        {
            return null;
        }

        var currentPane = listBox == ConversationListBox ? EventPane.Conversation : EventPane.Execution;
        var anchor = viewModel.CurrentTranscriptEvent is not null
            && viewModel.CurrentTranscriptEvent.Pane == currentPane
            && list.Contains(viewModel.CurrentTranscriptEvent)
            ? viewModel.CurrentTranscriptEvent
            : GetAnchorEvent(listBox, scrollViewer);

        int anchorIndex = anchor != null ? list.IndexOf(anchor) : -1;
        if (anchorIndex == -1)
        {
            anchorIndex = direction > 0 ? -1 : list.Count;
        }

        int targetIndex = Math.Clamp(anchorIndex + Math.Sign(direction), 0, list.Count - 1);
        return list[targetIndex] as DisplayEvent;
    }

    private DisplayEvent? GetAnchorEvent(ListBox listBox, ScrollViewer scrollViewer)
    {
        const double topTolerance = 12;
        var targetY = scrollViewer.Offset.Y + topTolerance;
        int count = listBox.Items.Count;
        if (count == 0) return null;

        int low = 0;
        int high = count - 1;
        int candidateIndex = 0;

        while (low <= high)
        {
            int mid = low + (high - low) / 2;
            var container = listBox.ContainerFromIndex(mid) as ListBoxItem;
            if (container is null || !container.IsVisible)
            {
                high = mid - 1;
                continue;
            }

            if (TryGetContainerExtentBounds(container, out var top, out _))
            {
                if (top <= targetY)
                {
                    candidateIndex = mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }
            else
            {
                high = mid - 1;
            }
        }

        return listBox.Items[candidateIndex] as DisplayEvent;
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
    private static void ScrollContainerToTop(ScrollViewer scrollViewer, ListBoxItem target)
    {
        if (TryGetContainerExtentBounds(target, out var top, out _))
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

    private static bool TryGetEventContainer(ListBox listBox, DisplayEvent evt, out ListBoxItem target)
    {
        var itemIndex = FindEventIndex(listBox, evt);
        if (itemIndex >= 0)
        {
            if (listBox.ContainerFromIndex(itemIndex) is ListBoxItem container)
            {
                target = container;
                return true;
            }
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

    private static int FindEventIndex(ListBox listBox, DisplayEvent evt)
    {
        var items = listBox.Items;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] is DisplayEvent de && string.Equals(de.Id, evt.Id, StringComparison.Ordinal))
            {
                return i;
            }
        }
        return -1;
    }

    private static bool TryGetContainerExtentBounds(
        ListBoxItem item,
        out double top,
        out double bottom)
    {
        if (item is null || item.Bounds.Height <= 0)
        {
            top = 0;
            bottom = 0;
            return false;
        }

        top = item.Bounds.Y;
        bottom = top + item.Bounds.Height;
        return true;
    }

    private static bool IsTextInputFocused(object? source)
    {
        return source is TextBox
            || source is Visual visual
            && visual.GetVisualAncestors().OfType<TextBox>().Any();
    }
}
