using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CodexLens.Models;
using CodexLens.ViewModels;
using SukiUI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodexLens.Views;

public partial class MainWindow : SukiWindow
{
    private SettingsWindow? _settingsWindow;

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

        var keyText = ShortcutKeyInput.ToShortcutKeyText(e.Key);
        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        var alt = e.KeyModifiers.HasFlag(KeyModifiers.Alt);

        if (viewModel.IsCapturingSyncShortcut)
        {
            if (ShortcutKeyInput.IsModifierOnlyKey(e.Key))
            {
                e.Handled = true;
                return;
            }

            if (keyText.Length > 0)
            {
                viewModel.CaptureSyncShortcut(ctrl, shift, alt, keyText);
            }
            else
            {
                viewModel.RejectSyncShortcutCapture("Shortcut must be Ctrl/Shift/Alt + another key.");
            }

            e.Handled = true;
            return;
        }

        if (IsTextInputFocused(e.Source))
        {
            return;
        }

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
        var (itemsControl, scrollViewer) = ControlsForPane(effectivePane);

        if (!viewModel.IsSynchronizedNavigationEnabled)
        {
            var target = ScrollToAdjacentMessage(itemsControl, scrollViewer, direction);
            viewModel.SetCurrentTranscriptEvent(target);
            return;
        }

        var anchor = GetAnchorEvent(itemsControl, scrollViewer);
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
        ItemsControl itemsControl,
        ScrollViewer scrollViewer,
        int direction)
    {
        var containers = GetItemContainers(itemsControl);

        if (containers.Count == 0)
        {
            return null;
        }

        var targetIndex = GetAdjacentIndex(itemsControl, scrollViewer, containers, direction);
        var target = containers[targetIndex];
        ScrollContainerToTop(itemsControl, scrollViewer, target);
        return target.DataContext as DisplayEvent;
    }

    private DisplayEvent? GetAnchorEvent(ItemsControl itemsControl, ScrollViewer scrollViewer)
    {
        var containers = GetItemContainers(itemsControl);

        if (containers.Count == 0)
        {
            return null;
        }

        const double topTolerance = 12;
        ContentPresenter? candidate = null;

        foreach (var container in containers)
        {
            var top = GetContainerTop(itemsControl, container);
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
        var (itemsControl, scrollViewer) = ControlsForPane(evt.Pane);

        var target = GetItemContainers(itemsControl)
            .FirstOrDefault(x => IsEventContainer(x, evt));

        if (target is not null)
        {
            ScrollContainerToTop(itemsControl, scrollViewer, target);
        }
    }

    private (ItemsControl ItemsControl, ScrollViewer ScrollViewer) ControlsForPane(EventPane pane)
    {
        return pane == EventPane.Execution
            ? (ExecutionItemsControl, ExecutionScrollViewer)
            : (ConversationItemsControl, ConversationScrollViewer);
    }

    private static List<ContentPresenter> GetItemContainers(ItemsControl itemsControl)
    {
        return itemsControl
            .GetVisualDescendants()
            .OfType<ContentPresenter>()
            .Where(x =>
                x.IsVisible &&
                x.Bounds.Height > 0 &&
                x.DataContext is DisplayEvent)
            .OrderBy(x =>
            {
                var point = x.TranslatePoint(new Point(0, 0), itemsControl);
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
        ItemsControl itemsControl,
        ScrollViewer scrollViewer,
        IReadOnlyList<ContentPresenter> containers,
        int direction)
    {
        return direction > 0
            ? GetNextIndex(itemsControl, scrollViewer, containers)
            : GetPreviousIndex(itemsControl, scrollViewer, containers);
    }

    private static int GetNextIndex(
        ItemsControl itemsControl,
        ScrollViewer scrollViewer,
        IReadOnlyList<ContentPresenter> containers)
    {
        const double topTolerance = 12;
        var threshold = scrollViewer.Offset.Y + topTolerance;

        for (var i = 0; i < containers.Count; i++)
        {
            var top = GetContainerTop(itemsControl, containers[i]);
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
        ItemsControl itemsControl,
        ScrollViewer scrollViewer,
        IReadOnlyList<ContentPresenter> containers)
    {
        const double topTolerance = 6;
        var threshold = scrollViewer.Offset.Y - topTolerance;

        for (var i = containers.Count - 1; i >= 0; i--)
        {
            var top = GetContainerTop(itemsControl, containers[i]);
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
        ItemsControl itemsControl,
        ScrollViewer scrollViewer,
        ContentPresenter target)
    {
        var top = GetContainerTop(itemsControl, target);

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

    private static double? GetContainerTop(ItemsControl itemsControl, ContentPresenter target)
    {
        return target.TranslatePoint(new Point(0, 0), itemsControl)?.Y;
    }

    private static bool IsTextInputFocused(object? source)
    {
        return source is TextBox
            || source is Visual visual
            && visual.GetVisualAncestors().OfType<TextBox>().Any();
    }

}
