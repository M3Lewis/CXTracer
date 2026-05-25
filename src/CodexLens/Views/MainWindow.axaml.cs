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

            var anchorIndex = GetTopVisibleIndex(scrollViewer, containers);

            var targetIndex = anchorIndex < 0
                ? direction > 0 ? 0 : containers.Count - 1
                : Math.Clamp(anchorIndex + direction, 0, containers.Count - 1);

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
                var p = x.TranslatePoint(new Point(0, 0), itemsControl);
                return p?.Y ?? double.MaxValue;
            })
            .ToList();
    }

    private static int GetTopVisibleIndex(
        ScrollViewer scrollViewer,
        IReadOnlyList<ContentPresenter> containers)
    {
        const double tolerance = 6;

        for (var i = 0; i < containers.Count; i++)
        {
            var point = containers[i].TranslatePoint(new Point(0, 0), scrollViewer);

            if (point is null)
            {
                continue;
            }

            var top = point.Value.Y;
            var bottom = top + containers[i].Bounds.Height;

            // 第一个没有完全滚出顶部的卡片，就是当前锚点
            if (bottom > tolerance)
            {
                return i;
            }
        }

        return containers.Count - 1;
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

        // 关键修复：
        // point.Value.Y 是目标卡片相对当前 ScrollViewer 视口的位置，
        // 所以必须加上当前 Offset，而不是直接把 point.Value.Y 当成 Offset。
        var desiredY = scrollViewer.Offset.Y + point.Value.Y - topPadding;

        var maxY = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
        var clampedY = Math.Clamp(desiredY, 0, maxY);

        scrollViewer.Offset = new Vector(scrollViewer.Offset.X, clampedY);
    }
}