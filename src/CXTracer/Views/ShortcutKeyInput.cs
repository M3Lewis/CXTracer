using Avalonia.Input;

namespace CXTracer.Views;

internal static class ShortcutKeyInput
{
    public static bool IsModifierOnlyKey(Key key)
    {
        return key is Key.LeftCtrl
            or Key.RightCtrl
            or Key.LeftShift
            or Key.RightShift
            or Key.LeftAlt
            or Key.RightAlt;
    }

    public static string ToShortcutKeyText(Key key)
    {
        var text = key.ToString();
        if (text.Length == 1 && char.IsLetterOrDigit(text[0]))
        {
            return text.ToUpperInvariant();
        }

        return key switch
        {
            Key.D0 => "0",
            Key.D1 => "1",
            Key.D2 => "2",
            Key.D3 => "3",
            Key.D4 => "4",
            Key.D5 => "5",
            Key.D6 => "6",
            Key.D7 => "7",
            Key.D8 => "8",
            Key.D9 => "9",
            Key.OemQuotes or Key.Oem7 => "'",
            Key.OemSemicolon or Key.Oem1 => ";",
            Key.OemComma => ",",
            Key.OemPeriod => ".",
            Key.OemMinus => "-",
            Key.OemPlus => "=",
            Key.OemQuestion or Key.Oem2 => "/",
            Key.OemTilde or Key.Oem3 => "`",
            Key.OemOpenBrackets or Key.Oem4 => "[",
            Key.OemCloseBrackets or Key.Oem6 => "]",
            Key.OemPipe or Key.Oem5 => "\\",
            Key.OemBackslash or Key.Oem102 => "\\",
            Key.Space => "Space",
            Key.Tab => "Tab",
            Key.Back => "Backspace",
            Key.Delete => "Delete",
            Key.Insert => "Insert",
            Key.Home => "Home",
            Key.End => "End",
            Key.PageUp or Key.Prior => "PageUp",
            Key.PageDown or Key.Next => "PageDown",
            Key.Left => "Left",
            Key.Right => "Right",
            Key.Up => "Up",
            Key.Down => "Down",
            Key.Return or Key.Enter => "Enter",
            Key.Escape => "Esc",
            Key.NumPad0 => "NumPad0",
            Key.NumPad1 => "NumPad1",
            Key.NumPad2 => "NumPad2",
            Key.NumPad3 => "NumPad3",
            Key.NumPad4 => "NumPad4",
            Key.NumPad5 => "NumPad5",
            Key.NumPad6 => "NumPad6",
            Key.NumPad7 => "NumPad7",
            Key.NumPad8 => "NumPad8",
            Key.NumPad9 => "NumPad9",
            Key.Add => "NumPadAdd",
            Key.Subtract => "NumPadSubtract",
            Key.Multiply => "NumPadMultiply",
            Key.Divide => "NumPadDivide",
            Key.Decimal => "NumPadDecimal",
            _ when text.StartsWith("F", System.StringComparison.Ordinal)
                && text.Length is >= 2 and <= 3
                && int.TryParse(text[1..], out var functionNumber)
                && functionNumber is >= 1 and <= 24 => text,
            _ => string.Empty
        };
    }
}
