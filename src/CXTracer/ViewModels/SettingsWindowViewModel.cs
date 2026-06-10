using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel;

namespace CXTracer.ViewModels;

public sealed partial class SettingsWindowViewModel : ObservableObject, IDisposable
{
    private readonly MainWindowViewModel _main;
    private bool _isSynchronizedNavigationEnabled;
    private string _statusMessage;

    public static string[] LanguageOptions { get; } = ["en", "zh"];
    public static string[] LanguageDisplayNames { get; } = ["English", "简体中文"];

    public bool? IsSynchronizedNavigationEnabled
    {
        get => _isSynchronizedNavigationEnabled;
        set
        {
            if (value is not bool enabled || _isSynchronizedNavigationEnabled == enabled)
            {
                return;
            }

            _isSynchronizedNavigationEnabled = enabled;
            _main.IsSynchronizedNavigationEnabled = enabled;
            StatusMessage = enabled
                ? _main.L("SyncNavEnabled", "Synchronized navigation enabled.")
                : _main.L("SyncNavDisabled", "Synchronized navigation disabled.");
            OnPropertyChanged();
        }
    }

    public bool MinimizeToTray
    {
        get => _main.MinimizeToTray;
        set
        {
            if (_main.MinimizeToTray == value) return;
            _main.MinimizeToTray = value;
            OnPropertyChanged();
        }
    }

    public bool CloseToTray
    {
        get => _main.CloseToTray;
        set
        {
            if (_main.CloseToTray == value) return;
            _main.CloseToTray = value;
            OnPropertyChanged();
        }
    }

    public bool ExpandAllEventsByDefault
    {
        get => _main.ExpandAllEventsByDefault;
        set
        {
            if (_main.ExpandAllEventsByDefault == value) return;
            _main.ExpandAllEventsByDefault = value;
            OnPropertyChanged();
        }
    }

    public string CurrentLanguage
    {
        get => _main.CurrentLanguage;
        set
        {
            if (_main.CurrentLanguage == value) return;
            _main.CurrentLanguage = value;
            OnPropertyChanged();
            // Refresh display labels after language swap
            OnPropertyChanged(nameof(ShortcutEditorText));
            StatusMessage = _main.L("StatusReady", "Ready");
        }
    }

    public int SelectedLanguageIndex
    {
        get => CurrentLanguage == "zh" ? 1 : 0;
        set
        {
            CurrentLanguage = value == 1 ? "zh" : "en";
            OnPropertyChanged();
        }
    }

    public string ShortcutEditorText => _main.SyncShortcutEditorText;
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsCapturingSyncShortcut => _main.IsCapturingSyncShortcut;

    public SettingsWindowViewModel(MainWindowViewModel main)
    {
        _main = main;
        _isSynchronizedNavigationEnabled = main.IsSynchronizedNavigationEnabled;
        _statusMessage = main.L("StatusSettingsReady", "Settings ready.");
        _main.PropertyChanged += OnMainPropertyChanged;
    }

    [RelayCommand]
    private void StartSyncShortcutCapture()
    {
        _main.StartSyncShortcutCapture();
        StatusMessage = _main.StatusMessage;
    }

    [RelayCommand(CanExecute = nameof(CanConfirmSyncShortcut))]
    private void ConfirmSyncShortcut()
    {
        _main.ConfirmPendingSyncShortcut();
        StatusMessage = _main.StatusMessage;
    }

    private bool CanConfirmSyncShortcut()
    {
        return _main.CanConfirmPendingSyncShortcut();
    }

    [RelayCommand]
    private void OpenGithub()
    {
        try
        {
            var url = "https://github.com/M3Lewis";
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                System.Diagnostics.Process.Start("xdg-open", url);
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
            {
                System.Diagnostics.Process.Start("open", url);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = _main.LF("StatusOpenLinkFailed", "Failed to open link: {0}", ex.Message);
        }
    }

    public void CaptureSyncShortcut(bool ctrl, bool shift, bool alt, string keyText)
    {
        _main.CaptureSyncShortcut(ctrl, shift, alt, keyText);
        StatusMessage = _main.StatusMessage;
    }

    public void RejectSyncShortcutCapture(string message)
    {
        _main.RejectSyncShortcutCapture(message);
        StatusMessage = message;
    }

    private void OnMainPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainWindowViewModel.IsSynchronizedNavigationEnabled):
                if (_isSynchronizedNavigationEnabled != _main.IsSynchronizedNavigationEnabled)
                {
                    _isSynchronizedNavigationEnabled = _main.IsSynchronizedNavigationEnabled;
                    OnPropertyChanged(nameof(IsSynchronizedNavigationEnabled));
                }
                break;
            case nameof(MainWindowViewModel.SyncShortcutEditorText):
            case nameof(MainWindowViewModel.SyncNavigationShortcutText):
            case nameof(MainWindowViewModel.IsCapturingSyncShortcut):
            case nameof(MainWindowViewModel.PendingSyncShortcutText):
                OnPropertyChanged(nameof(ShortcutEditorText));
                OnPropertyChanged(nameof(IsCapturingSyncShortcut));
                ConfirmSyncShortcutCommand.NotifyCanExecuteChanged();
                break;
            case nameof(MainWindowViewModel.CurrentLanguage):
                OnPropertyChanged(nameof(CurrentLanguage));
                OnPropertyChanged(nameof(SelectedLanguageIndex));
                break;
            case nameof(MainWindowViewModel.MinimizeToTray):
                OnPropertyChanged(nameof(MinimizeToTray));
                break;
            case nameof(MainWindowViewModel.CloseToTray):
                OnPropertyChanged(nameof(CloseToTray));
                break;
            case nameof(MainWindowViewModel.ExpandAllEventsByDefault):
                OnPropertyChanged(nameof(ExpandAllEventsByDefault));
                break;
        }
    }

    public void Dispose()
    {
        _main.PropertyChanged -= OnMainPropertyChanged;
    }
}
