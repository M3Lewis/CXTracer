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
        if (DataContext is not SettingsWindowViewModel viewModel
            || !viewModel.IsCapturingSyncShortcut)
        {
            return;
        }

        var keyText = ShortcutKeyInput.ToShortcutKeyText(e.Key);
        if (ShortcutKeyInput.IsModifierOnlyKey(e.Key))
        {
            e.Handled = true;
            return;
        }

        if (keyText.Length > 0)
        {
            viewModel.CaptureSyncShortcut(
                e.KeyModifiers.HasFlag(KeyModifiers.Control),
                e.KeyModifiers.HasFlag(KeyModifiers.Shift),
                e.KeyModifiers.HasFlag(KeyModifiers.Alt),
                keyText);
        }
        else
        {
            viewModel.RejectSyncShortcutCapture("Shortcut must be Ctrl/Shift/Alt + another key.");
        }

        e.Handled = true;
    }

    private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

}
