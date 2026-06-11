using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SukiUI.Controls;
using System;
using System.ComponentModel;
using CXTracer.Models;
using CXTracer.ViewModels;

namespace CXTracer.Views;

public partial class MainWindow : SukiWindow
{
    private SettingsWindow? _settingsWindow;
    private ScrollViewer? _conversationScrollViewer;
    private ScrollViewer? _executionScrollViewer;
    private MainWindowViewModel? _registeredViewModel;
    private TrayIcon? _trayIcon;
    private NativeMenuItem? _openMenuItem;
    private NativeMenuItem? _exitMenuItem;
    private bool _isExiting;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_registeredViewModel is not null)
        {
            _registeredViewModel.FilterAppliedScrollRequest -= ViewModel_FilterAppliedScrollRequest;
            _registeredViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        if (DataContext is MainWindowViewModel viewModel)
        {
            _registeredViewModel = viewModel;
            viewModel.FilterAppliedScrollRequest += ViewModel_FilterAppliedScrollRequest;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;

            if (_trayIcon is null)
            {
                InitializeTrayIcon();
            }
            else
            {
                UpdateTrayText();
            }
        }
        else
        {
            _registeredViewModel = null;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.CurrentLanguage))
        {
            UpdateTrayText();
        }
    }

    private void Settings_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            if (_settingsWindow is { IsVisible: true })
            {
                _settingsWindow.Activate();
                return;
            }

            var settingsVm = new SettingsWindowViewModel(viewModel);
            _settingsWindow = new SettingsWindow
            {
                DataContext = settingsVm
            };
            _settingsWindow.Closed += (_, _) =>
            {
                _settingsWindow = null;
                settingsVm.Dispose();
            };
            _settingsWindow.Show(this);
        }
    }
}
