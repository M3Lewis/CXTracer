using CodexLens.Models;

namespace CodexLens.Services;

public sealed class AppSettings
{
    public bool IsSynchronizedNavigationEnabled { get; set; }
    public ShortcutGesture? SyncNavigationShortcut { get; set; }
}
