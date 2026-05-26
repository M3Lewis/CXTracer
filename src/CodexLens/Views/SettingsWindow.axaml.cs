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

        var letter = KeyToLetter(e.Key);
        if (letter.Length == 1)
        {
            viewModel.CaptureSyncShortcut(
                e.KeyModifiers.HasFlag(KeyModifiers.Control),
                e.KeyModifiers.HasFlag(KeyModifiers.Shift),
                e.KeyModifiers.HasFlag(KeyModifiers.Alt),
                letter);
        }
        else
        {
            viewModel.RejectSyncShortcutCapture("Shortcut must be Ctrl/Shift/Alt + a letter.");
        }

        e.Handled = true;
    }

    private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private static string KeyToLetter(Key key)
    {
        var text = key.ToString();
        return text.Length == 1 && char.IsLetter(text[0])
            ? text.ToUpperInvariant()
            : string.Empty;
    }
}
