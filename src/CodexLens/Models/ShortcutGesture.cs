using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CodexLens.Models;

public sealed class ShortcutGesture
{
    public bool Ctrl { get; init; }
    public bool Shift { get; init; }
    public bool Alt { get; init; }
    public string Letter { get; init; } = string.Empty;

    [JsonIgnore]
    public bool IsValid => (Ctrl || Shift || Alt)
        && Letter.Length == 1
        && char.IsLetter(Letter[0]);

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

            parts.Add(Letter.ToUpperInvariant());
            return string.Join("+", parts);
        }
    }

    public bool Matches(bool ctrl, bool shift, bool alt, string letter)
    {
        return IsValid
            && Ctrl == ctrl
            && Shift == shift
            && Alt == alt
            && string.Equals(Letter, letter, StringComparison.OrdinalIgnoreCase);
    }

    public static ShortcutGesture Create(bool ctrl, bool shift, bool alt, string letter)
    {
        return new ShortcutGesture
        {
            Ctrl = ctrl,
            Shift = shift,
            Alt = alt,
            Letter = letter.Length == 0
                ? string.Empty
                : letter[..1].ToUpperInvariant()
        };
    }
}
