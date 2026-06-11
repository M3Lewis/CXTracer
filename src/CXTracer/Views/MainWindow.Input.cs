using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System;
using System.Linq;
using System.Threading.Tasks;
using SukiUI.Toasts;
using CXTracer.Models;
using CXTracer.ViewModels;

namespace CXTracer.Views;

public partial class MainWindow
{
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

    private static bool IsTextInputFocused(object? source)
    {
        return source is TextBox
            || source is Visual visual
            && visual.GetVisualAncestors().OfType<TextBox>().Any();
    }
}
