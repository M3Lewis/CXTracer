using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CXTracer.Models;

namespace CXTracer.ViewModels;

public sealed partial class MainWindowViewModel
{
    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = L("StatusScanning", "Scanning sessions...");
            ResetEvents();
            Sessions.Clear();
            _allSessions.Clear();
            OnPropertyChanged(nameof(SessionCountText));

            var normalized = NormalizeRoot(RootPath);
            RootPath = normalized;

            if (!Directory.Exists(normalized))
            {
                StatusMessage = $"Directory not found: {normalized}";
                _watcher.Stop();
                return;
            }

            var sessions = await Task.Run(() => _scanner.ScanLight(normalized)).ConfigureAwait(true);
            _allSessions.AddRange(sessions);
            await PopulateSessionsFilteredAsync(System.Threading.CancellationToken.None).ConfigureAwait(true);

            _enrichCts?.Cancel();
            _enrichCts = new System.Threading.CancellationTokenSource();
            _ = StartBackgroundEnrichmentAsync(_enrichCts.Token);

            _watcher.Start(normalized);
            StatusMessage = sessions.Count == 0
                ? L("StatusNoSessions", "No .jsonl sessions found.")
                : LF("StatusLoadedSessions", "Loaded {0} session entries.", sessions.Count);

        }
        catch (Exception ex)
        {
            StatusMessage = LF("StatusScanFailed", "Scan failed: {0}", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ClearSearch()
    {
        EventSearchText = string.Empty;
    }

    [RelayCommand]
    private void OpenDefaultRoot()
    {
        RootPath = Services.SessionScanner.DefaultRootPath();
        _ = RefreshAsync();
    }

    [RelayCommand]
    private void ToggleRaw()
    {
        ShowRawEvents = !ShowRawEvents;
    }

    [RelayCommand]
    public void ShowImageViewer(string? imagePath)
    {
        if (string.IsNullOrEmpty(imagePath)) return;
        ViewerImagePath = imagePath;
        IsImageViewerOpen = true;
    }

    [RelayCommand]
    public void CloseImageViewer()
    {
        IsImageViewerOpen = false;
        ViewerImagePath = null;
    }

    [RelayCommand]
    public void ToggleImageSize()
    {
        ViewerImageStretch = ViewerImageStretch == "None" ? "Uniform" : "None";
    }

    [RelayCommand]
    private void ConfirmSyncShortcut()
    {
        ConfirmPendingSyncShortcut();
    }

    public void ConfirmPendingSyncShortcut()
    {
        var gesture = ParseShortcutText(PendingSyncShortcutText);
        if (gesture is null)
        {
            StatusMessage = L("SyncNavChooseFirst", "Choose a Ctrl/Shift/Alt + key shortcut first.");
            return;
        }

        _syncNavigationShortcut = gesture;
        PendingSyncShortcutText = string.Empty;
        IsCapturingSyncShortcut = false;
        OnPropertyChanged(nameof(SyncNavigationShortcutText));
        OnPropertyChanged(nameof(SyncShortcutEditorText));
        SaveSettings();
        StatusMessage = LF("SyncNavSet", "Sync navigation shortcut set to {0}.", gesture.DisplayText);
    }

    private bool CanConfirmSyncShortcut()
    {
        return CanConfirmPendingSyncShortcut();
    }

    public bool CanConfirmPendingSyncShortcut()
    {
        return ParseShortcutText(PendingSyncShortcutText) is not null;
    }

    public void StartSyncShortcutCapture()
    {
        PendingSyncShortcutText = string.Empty;
        IsCapturingSyncShortcut = true;
        StatusMessage = L("SyncNavPressKey", "Press Ctrl/Shift/Alt + a key for sync navigation.");
    }

    public void CaptureSyncShortcut(bool ctrl, bool shift, bool alt, string keyText)
    {
        var gesture = ShortcutGesture.Create(ctrl, shift, alt, keyText);
        if (!gesture.IsValid)
        {
            RejectSyncShortcutCapture("Shortcut must be Ctrl/Shift/Alt + another key.");
            return;
        }

        PendingSyncShortcutText = gesture.DisplayText;
        IsCapturingSyncShortcut = false;
        StatusMessage = LF("SyncNavCaptured", "Captured {0}. Click OK to save.", gesture.DisplayText);
    }

    public void RejectSyncShortcutCapture(string message)
    {
        IsCapturingSyncShortcut = false;
        StatusMessage = message;
    }

    public bool MatchesSyncNavigationShortcut(bool ctrl, bool shift, bool alt, string keyText)
    {
        return _syncNavigationShortcut?.Matches(ctrl, shift, alt, keyText) == true;
    }

    public void ToggleSynchronizedNavigation()
    {
        IsSynchronizedNavigationEnabled = !IsSynchronizedNavigationEnabled;
        StatusMessage = IsSynchronizedNavigationEnabled
            ? L("SyncNavEnabled", "Synchronized navigation enabled.")
            : L("SyncNavDisabled", "Synchronized navigation disabled.");
    }

    private static ShortcutGesture? ParseShortcutText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var parts = value.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            return null;
        }

        var ctrl = parts.Any(x => string.Equals(x, "Ctrl", StringComparison.OrdinalIgnoreCase));
        var shift = parts.Any(x => string.Equals(x, "Shift", StringComparison.OrdinalIgnoreCase));
        var alt = parts.Any(x => string.Equals(x, "Alt", StringComparison.OrdinalIgnoreCase));
        var keyText = parts.LastOrDefault(x => !ShortcutGesture.IsModifierText(x)) ?? string.Empty;
        var gesture = ShortcutGesture.Create(ctrl, shift, alt, keyText);
        return gesture.IsValid ? gesture : null;
    }
}
