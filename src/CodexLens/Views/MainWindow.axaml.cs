using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CodexLens.Models;
using SukiUI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodexLens.Views;

public partial class MainWindow : SukiWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void ConversationUp_Click(object? sender, RoutedEventArgs e)
    {
        ScrollToAdjacentMessage(ConversationItemsControl, ConversationScrollViewer, -1);
    }

    private void ConversationDown_Click(object? sender, RoutedEventArgs e)
    {
        ScrollToAdjacentMessage(ConversationItemsControl, ConversationScrollViewer, 1);
    }

    private void ExecutionUp_Click(object? sender, RoutedEventArgs e)
    {
        ScrollToAdjacentMessage(ExecutionItemsControl, ExecutionScrollViewer, -1);
    }

    private void ExecutionDown_Click(object? sender, RoutedEventArgs e)
    {
        ScrollToAdjacentMessage(ExecutionItemsControl, ExecutionScrollViewer, 1);
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

        scrollViewer.Offset = new Vector(scrollViewer.Offset.X, clampedY);
    }
}
