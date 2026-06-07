using CXTracer.Models;

namespace CXTracer.Services;

public sealed class AppSettings
{
    public bool IsSynchronizedNavigationEnabled { get; set; }
    public ShortcutGesture? SyncNavigationShortcut { get; set; }
    public string Language { get; set; } = "en";
}
