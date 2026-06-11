using Avalonia.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;
using CXTracer.Models;
using CXTracer.ViewModels;

namespace CXTracer.ViewModels;

public sealed partial class MainWindowViewModel
{
    partial void OnRootPathChanged(string value)
    {
        StatusMessage = System.IO.Directory.Exists(value)
            ? L("StatusDirExists", "Directory exists. Click Refresh to scan.")
            : LF("StatusDirNotFound", "Directory not found: {0}", value);
    }

    partial void OnSelectedSessionChanged(SessionInfo? value)
    {
        OnPropertyChanged(nameof(SelectedSessionPath));

        if (_selectionChanging || value is null)
        {
            return;
        }

        _ = LoadSelectedSessionAsync(value);
    }

    partial void OnSessionSearchTextChanged(string value)
    {
        _sessionFilterCts?.Cancel();
        _sessionFilterCts?.Dispose();
        _sessionFilterCts = new CancellationTokenSource();
        var ct = _sessionFilterCts.Token;
        _ = FilterSessionsAndEnrichAsync(ct);
    }

    partial void OnEventSearchTextChanged(string value)
    {
        _eventFilterCts?.Cancel();
        _eventFilterCts?.Dispose();
        _eventFilterCts = new CancellationTokenSource();
        var ct = _eventFilterCts.Token;
        _ = ApplyFilterAsync(ct, debounce: true);
    }

    partial void OnSelectedFilterChanged(FilterOptionItem? value)
    {
        _eventFilterCts?.Cancel();
        _eventFilterCts?.Dispose();
        _eventFilterCts = new CancellationTokenSource();
        var ct = _eventFilterCts.Token;
        _ = ApplyFilterAsync(ct, debounce: false);
    }

    partial void OnShowRawEventsChanged(bool value)
    {
        _eventFilterCts?.Cancel();
        _eventFilterCts?.Dispose();
        _eventFilterCts = new CancellationTokenSource();
        var ct = _eventFilterCts.Token;
        _ = ApplyFilterAsync(ct, debounce: false);
    }

    partial void OnCurrentLanguageChanged(string value)
    {
        ApplyLanguage(value);
        if (!_isLoadingSettings)
        {
            SaveSettings();
        }
    }
    partial void OnActiveTranscriptPaneChanged(EventPane value)
    {
        OnPropertyChanged(nameof(IsConversationPaneActive));
        OnPropertyChanged(nameof(IsExecutionPaneActive));
    }

    partial void OnCurrentTranscriptEventChanged(DisplayEvent? oldValue, DisplayEvent? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.IsCurrentNavigationTarget = false;
        }

        if (newValue is not null)
        {
            newValue.IsCurrentNavigationTarget = true;
            SetActiveTranscriptPane(newValue.Pane);
        }
    }

    partial void OnIsCapturingSyncShortcutChanged(bool value) => OnPropertyChanged(nameof(SyncShortcutEditorText));
    partial void OnPendingSyncShortcutTextChanged(string value) => OnPropertyChanged(nameof(SyncShortcutEditorText));
    partial void OnDetailPopupEventChanged(DisplayEvent? value) => OnPropertyChanged(nameof(IsDetailPopupOpen));

    public void ShowDetailPopup(DisplayEvent evt) => DetailPopupEvent = evt;
    public void CloseDetailPopup() => DetailPopupEvent = null;
    partial void OnIsSynchronizedNavigationEnabledChanged(bool value)
    {
        if (!_isLoadingSettings)
        {
            SaveSettings();
        }
    }

    partial void OnMinimizeToTrayChanged(bool value)
    {
        if (!_isLoadingSettings)
        {
            SaveSettings();
        }
    }

    partial void OnCloseToTrayChanged(bool value)
    {
        if (!_isLoadingSettings)
        {
            SaveSettings();
        }
    }

    partial void OnExpandAllEventsByDefaultChanged(bool value)
    {
        if (!_isLoadingSettings)
        {
            SaveSettings();
        }
        foreach (var evt in _allEvents)
        {
            evt.ResetExpansionState(value);
        }
    }

    partial void OnViewerImageStretchChanged(string value)
    {
        OnPropertyChanged(nameof(ViewerToggleText));
    }
}
