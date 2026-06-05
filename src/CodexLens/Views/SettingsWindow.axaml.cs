using Avalonia.Controls;
using Avalonia.Input;
using CodexLens.ViewModels;
using SukiUI.Controls;
using System;

namespace CodexLens.Views;

public partial class SettingsWindow : SukiWindow
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnClosed(e);
    }

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not SettingsWindowViewModel viewModel)
        {
            return;
        }

        _ = ShortcutCaptureInput.TryHandleCapture(
            e,
            viewModel.IsCapturingSyncShortcut,
            viewModel.CaptureSyncShortcut,
            viewModel.RejectSyncShortcutCapture);
    }

    private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

}
