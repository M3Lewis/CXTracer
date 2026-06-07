using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using SukiUI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
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

            _settingsWindow = new SettingsWindow
            {
                DataContext = new SettingsWindowViewModel(viewModel)
            };
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            _settingsWindow.Show(this);
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
            var target = ScrollToAdjacentMessage(listBox, scrollViewer, direction);
            viewModel.SetCurrentTranscriptEvent(target);
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

    private static DisplayEvent? ScrollToAdjacentMessage(
        ListBox listBox,
        ScrollViewer scrollViewer,
        int direction)
    {
        var containers = GetItemContainers(listBox);

        if (containers.Count == 0)
        {
            return null;
        }

        var targetIndex = GetAdjacentIndex(listBox, scrollViewer, containers, direction);
        var target = containers[targetIndex];
        ScrollContainerToTop(listBox, scrollViewer, target);
        return target.DataContext as DisplayEvent;
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
            var top = GetContainerTop(listBox, container);
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

        if (scrollViewer is null)
        {
            return;
        }

        // First, use ListBox.ScrollIntoView to ensure the item is realized.
        var itemIndex = listBox.Items.Cast<object>().ToList().FindIndex(x =>
            x is DisplayEvent de && string.Equals(de.Id, evt.Id, StringComparison.Ordinal));

        if (itemIndex >= 0)
        {
            listBox.ScrollIntoView(itemIndex);
        }

        // Then fine-tune to top-align the container.
        var target = GetItemContainers(listBox)
            .FirstOrDefault(x => IsEventContainer(x, evt));

        if (target is not null)
        {
            ScrollContainerToTop(listBox, scrollViewer, target);
        }
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
        // With VirtualizingStackPanel, only realized (on-screen + buffer) containers
        // exist in the visual tree. This is intentional — navigation only needs the
        // visible subset, and the list is already in visual order.
        return listBox
            .GetVisualDescendants()
            .OfType<ContentPresenter>()
            .Where(x =>
                x.IsVisible &&
                x.Bounds.Height > 0 &&
                x.DataContext is DisplayEvent)
            .OrderBy(x =>
            {
                var point = x.TranslatePoint(new Point(0, 0), listBox);
                return point?.Y ?? double.MaxValue;
            })
            .ToList();
    }

    private static bool IsEventContainer(ContentPresenter container, DisplayEvent evt)
    {
        return ReferenceEquals(container.DataContext, evt)
            || container.DataContext is DisplayEvent candidate
            && string.Equals(candidate.Id, evt.Id, StringComparison.Ordinal);
    }

    private static int GetAdjacentIndex(
        ListBox listBox,
        ScrollViewer scrollViewer,
        IReadOnlyList<ContentPresenter> containers,
        int direction)
    {
        return direction > 0
            ? GetNextIndex(listBox, scrollViewer, containers)
            : GetPreviousIndex(listBox, scrollViewer, containers);
    }

    private static int GetNextIndex(
        ListBox listBox,
        ScrollViewer scrollViewer,
        IReadOnlyList<ContentPresenter> containers)
    {
        const double topTolerance = 12;
        var threshold = scrollViewer.Offset.Y + topTolerance;

        for (var i = 0; i < containers.Count; i++)
        {
            var top = GetContainerTop(listBox, containers[i]);
            if (top is null)
            {
                continue;
            }

            if (top.Value > threshold)
            {
                return i;
            }
        }

        return containers.Count - 1;
    }

    private static int GetPreviousIndex(
        ListBox listBox,
        ScrollViewer scrollViewer,
        IReadOnlyList<ContentPresenter> containers)
    {
        const double topTolerance = 6;
        var threshold = scrollViewer.Offset.Y - topTolerance;

        for (var i = containers.Count - 1; i >= 0; i--)
        {
            var top = GetContainerTop(listBox, containers[i]);
            if (top is null)
            {
                continue;
            }

            if (top.Value < threshold)
            {
                return i;
            }
        }

        return 0;
    }

    private static void ScrollContainerToTop(
        ListBox listBox,
        ScrollViewer scrollViewer,
        ContentPresenter target)
    {
        var top = GetContainerTop(listBox, target);

        if (top is null)
        {
            target.BringIntoView();
            return;
        }

        const double topPadding = 8;
        var desiredY = top.Value - topPadding;
        var maxY = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
        var clampedY = Math.Clamp(desiredY, 0, maxY);

        scrollViewer.Offset = new Vector(0, clampedY);
    }

    private static double? GetContainerTop(ListBox listBox, ContentPresenter target)
    {
        return target.TranslatePoint(new Point(0, 0), listBox)?.Y;
    }

    private static bool IsTextInputFocused(object? source)
    {
        return source is TextBox
            || source is Visual visual
            && visual.GetVisualAncestors().OfType<TextBox>().Any();
    }

}
