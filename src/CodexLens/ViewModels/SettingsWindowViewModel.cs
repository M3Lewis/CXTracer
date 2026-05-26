using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel;

namespace CodexLens.ViewModels;

public sealed partial class SettingsWindowViewModel : ObservableObject, IDisposable
{
    private readonly MainWindowViewModel _main;

    public bool IsSynchronizedNavigationEnabled
    {
        get => _main.IsSynchronizedNavigationEnabled;
        set
        {
            if (_main.IsSynchronizedNavigationEnabled == value)
            {
                return;
            }

            _main.IsSynchronizedNavigationEnabled = value;
            OnPropertyChanged();
        }
    }

    public string ShortcutEditorText => _main.SyncShortcutEditorText;
    public string StatusMessage => _main.StatusMessage;
    public bool IsCapturingSyncShortcut => _main.IsCapturingSyncShortcut;

    public SettingsWindowViewModel(MainWindowViewModel main)
    {
        _main = main;
        _main.PropertyChanged += OnMainPropertyChanged;
    }

    [RelayCommand]
    private void StartSyncShortcutCapture()
    {
        _main.StartSyncShortcutCapture();
    }

    [RelayCommand(CanExecute = nameof(CanConfirmSyncShortcut))]
    private void ConfirmSyncShortcut()
    {
        _main.ConfirmPendingSyncShortcut();
    }

    private bool CanConfirmSyncShortcut()
    {
        return _main.CanConfirmPendingSyncShortcut();
    }

    public void CaptureSyncShortcut(bool ctrl, bool shift, bool alt, string letter)
    {
        _main.CaptureSyncShortcut(ctrl, shift, alt, letter);
    }

    public void RejectSyncShortcutCapture(string message)
    {
        _main.RejectSyncShortcutCapture(message);
    }

    private void OnMainPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainWindowViewModel.IsSynchronizedNavigationEnabled):
                OnPropertyChanged(nameof(IsSynchronizedNavigationEnabled));
                break;
            case nameof(MainWindowViewModel.SyncShortcutEditorText):
            case nameof(MainWindowViewModel.SyncNavigationShortcutText):
            case nameof(MainWindowViewModel.IsCapturingSyncShortcut):
            case nameof(MainWindowViewModel.PendingSyncShortcutText):
                OnPropertyChanged(nameof(ShortcutEditorText));
                OnPropertyChanged(nameof(IsCapturingSyncShortcut));
                ConfirmSyncShortcutCommand.NotifyCanExecuteChanged();
                break;
            case nameof(MainWindowViewModel.StatusMessage):
                OnPropertyChanged(nameof(StatusMessage));
                break;
        }
    }

    public void Dispose()
    {
        _main.PropertyChanged -= OnMainPropertyChanged;
    }
}
