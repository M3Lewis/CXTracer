using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel;

namespace CXTracer.ViewModels;

public sealed partial class SettingsWindowViewModel : ObservableObject, IDisposable
{
    private readonly MainWindowViewModel _main;
    private bool _isSynchronizedNavigationEnabled;
    private string _statusMessage = "Settings ready.";

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
                ? "Synchronized navigation enabled."
                : "Synchronized navigation disabled.";
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
            StatusMessage = $"Failed to open link: {ex.Message}";
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
        }
    }

    public void Dispose()
    {
        _main.PropertyChanged -= OnMainPropertyChanged;
    }
}
