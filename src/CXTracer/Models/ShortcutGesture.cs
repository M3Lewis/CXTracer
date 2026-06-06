using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CXTracer.Models;

public sealed class ShortcutGesture
{
    public bool Ctrl { get; init; }
    public bool Shift { get; init; }
    public bool Alt { get; init; }
    public string Letter { get; init; } = string.Empty;

    [JsonIgnore]
    public bool IsValid => (Ctrl || Shift || Alt)
        && IsShortcutKeyText(Letter);

    [JsonIgnore]
    public string DisplayText
    {
        get
        {
            if (!IsValid)
            {
                return "Unset";
            }

            var parts = new List<string>();
            if (Ctrl)
            {
                parts.Add("Ctrl");
            }

            if (Shift)
            {
                parts.Add("Shift");
            }

            if (Alt)
            {
                parts.Add("Alt");
            }

            parts.Add(FormatKeyText(Letter));
            return string.Join("+", parts);
        }
    }

    public bool Matches(bool ctrl, bool shift, bool alt, string keyText)
    {
        return IsValid
            && Ctrl == ctrl
            && Shift == shift
            && Alt == alt
            && string.Equals(Letter, NormalizeKeyText(keyText), StringComparison.OrdinalIgnoreCase);
    }

    public static ShortcutGesture Create(bool ctrl, bool shift, bool alt, string keyText)
    {
        return new ShortcutGesture
        {
            Ctrl = ctrl,
            Shift = shift,
            Alt = alt,
            Letter = NormalizeKeyText(keyText)
        };
    }

    public static bool IsModifierText(string value)
    {
        return string.Equals(value, "Ctrl", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "Shift", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "Alt", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsShortcutKeyText(string value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && !IsModifierText(value);
    }

    private static string NormalizeKeyText(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        return trimmed.Length == 1 && char.IsLetter(trimmed[0])
            ? trimmed.ToUpperInvariant()
            : trimmed;
    }

    private static string FormatKeyText(string value)
    {
        return value.Length == 1 && char.IsLetter(value[0])
            ? value.ToUpperInvariant()
            : value;
    }
}
