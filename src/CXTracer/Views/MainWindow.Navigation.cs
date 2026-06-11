using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Linq;
using CXTracer.Models;
using CXTracer.ViewModels;

namespace CXTracer.Views;

public partial class MainWindow
{
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
}
