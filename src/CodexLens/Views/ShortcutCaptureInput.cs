using Avalonia.Input;
using System;

namespace CodexLens.Views;

internal static class ShortcutCaptureInput
{
    private const string InvalidShortcutMessage = "Shortcut must be Ctrl/Shift/Alt + another key.";

    public static bool TryHandleCapture(
        KeyEventArgs e,
        bool isCapturing,
        Action<bool, bool, bool, string> capture,
        Action<string> reject)
    {
        if (!isCapturing)
        {
            return false;
        }

        var keyText = ShortcutKeyInput.ToShortcutKeyText(e.Key);
        if (ShortcutKeyInput.IsModifierOnlyKey(e.Key))
        {
            e.Handled = true;
            return true;
        }

        if (keyText.Length > 0)
        {
            capture(
                e.KeyModifiers.HasFlag(KeyModifiers.Control),
                e.KeyModifiers.HasFlag(KeyModifiers.Shift),
                e.KeyModifiers.HasFlag(KeyModifiers.Alt),
                keyText);
        }
        else
        {
            reject(InvalidShortcutMessage);
        }

        e.Handled = true;
        return true;
    }
}
