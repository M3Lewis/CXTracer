using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
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

        var letter = KeyToLetter(e.Key);
        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        var alt = e.KeyModifiers.HasFlag(KeyModifiers.Alt);

        if (viewModel.IsCapturingSyncShortcut)
        {
            if (letter.Length == 1)
            {
                viewModel.CaptureSyncShortcut(ctrl, shift, alt, letter);
            }
            else
            {
                viewModel.RejectSyncShortcutCapture("Shortcut must be Ctrl/Shift/Alt + a letter.");
            }

            e.Handled = true;
            return;
        }

        if (IsTextInputFocused(e.Source))
        {
            return;
        }

        if (letter.Length == 1 && viewModel.MatchesSyncNavigationShortcut(ctrl, shift, alt, letter))
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

        viewModel.SetActiveTranscriptPane(pane);
        var (itemsControl, scrollViewer) = ControlsForPane(pane);

        if (!viewModel.IsSynchronizedNavigationEnabled)
        {
            ScrollToAdjacentMessage(itemsControl, scrollViewer, direction);
            return;
        }

        var anchor = GetAnchorEvent(itemsControl, scrollViewer);
        var target = viewModel.GetSynchronizedNavigationTarget(pane, direction, anchor);
        ScrollNavigationTarget(target);
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

    private static void ScrollToAdjacentMessage(
        ItemsControl itemsControl,
        ScrollViewer scrollViewer,
        int direction)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var containers = GetItemContainers(itemsControl);

            if (containers.Count == 0)
            {
                return;
            }

            var targetIndex = GetAdjacentIndex(scrollViewer, containers, direction);
            ScrollContainerToTop(scrollViewer, containers[targetIndex]);
        }, DispatcherPriority.Background);
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
            var point = container.TranslatePoint(new Point(0, 0), scrollViewer);

            if (point is null)
            {
                continue;
            }

            if (point.Value.Y <= topTolerance)
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

        Dispatcher.UIThread.Post(() =>
        {
            var target = GetItemContainers(itemsControl)
                .FirstOrDefault(x => IsEventContainer(x, evt));

            if (target is not null)
            {
                ScrollContainerToTop(scrollViewer, target);
            }
        }, DispatcherPriority.Background);
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
        ScrollViewer scrollViewer,
        IReadOnlyList<ContentPresenter> containers,
        int direction)
    {
        return direction > 0
            ? GetNextIndex(scrollViewer, containers)
            : GetPreviousIndex(scrollViewer, containers);
    }

    private static int GetNextIndex(
        ScrollViewer scrollViewer,
        IReadOnlyList<ContentPresenter> containers)
    {
        const double topTolerance = 12;

        for (var i = 0; i < containers.Count; i++)
        {
            var point = containers[i].TranslatePoint(new Point(0, 0), scrollViewer);

            if (point is null)
            {
                continue;
            }

            if (point.Value.Y > topTolerance)
            {
                return i;
            }
        }

        return containers.Count - 1;
    }

    private static int GetPreviousIndex(
        ScrollViewer scrollViewer,
        IReadOnlyList<ContentPresenter> containers)
    {
        const double topTolerance = 6;

        for (var i = containers.Count - 1; i >= 0; i--)
        {
            var point = containers[i].TranslatePoint(new Point(0, 0), scrollViewer);

            if (point is null)
            {
                continue;
            }

            if (point.Value.Y < -topTolerance)
            {
                return i;
            }
        }

        return 0;
    }

    private static void ScrollContainerToTop(
        ScrollViewer scrollViewer,
        ContentPresenter target)
    {
        var point = target.TranslatePoint(new Point(0, 0), scrollViewer);

        if (point is null)
        {
            target.BringIntoView();
            return;
        }

        const double topPadding = 8;
        var desiredY = scrollViewer.Offset.Y + point.Value.Y - topPadding;
        var maxY = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
        var clampedY = Math.Clamp(desiredY, 0, maxY);

        scrollViewer.Offset = new Vector(0, clampedY);
    }

    private static bool IsTextInputFocused(object? source)
    {
        return source is TextBox
            || source is Visual visual
            && visual.GetVisualAncestors().OfType<TextBox>().Any();
    }

    private static string KeyToLetter(Key key)
    {
        var text = key.ToString();
        return text.Length == 1 && char.IsLetter(text[0])
            ? text.ToUpperInvariant()
            : string.Empty;
    }
}
