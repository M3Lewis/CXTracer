using Avalonia;
using Avalonia.Controls;
using CXTracer.ViewModels;

namespace CXTracer.Views;

public partial class MainWindow
{
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!_isExiting && DataContext is MainWindowViewModel viewModel && viewModel.CloseToTray)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        RemoveTrayIcon();
        base.OnClosing(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == WindowStateProperty)
        {
            var state = change.GetNewValue<WindowState>();
            if (state == WindowState.Minimized)
            {
                if (DataContext is MainWindowViewModel viewModel && viewModel.MinimizeToTray)
                {
                    Hide();
                }
            }
        }
    }

    private void RestoreWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitApplication()
    {
        _isExiting = true;
        Close();
    }

    private void InitializeTrayIcon()
    {
        var app = Application.Current;
        if (app is null) return;

        WindowIcon? appIcon = null;
        if (app.TryGetResource("AppIcon", null, out var iconObj) == true && iconObj is WindowIcon windowIcon)
        {
            appIcon = windowIcon;
        }

        _openMenuItem = new NativeMenuItem();
        _openMenuItem.Click += (s, e) => RestoreWindow();

        _exitMenuItem = new NativeMenuItem();
        _exitMenuItem.Click += (s, e) => ExitApplication();

        _trayIcon = new TrayIcon
        {
            Icon = appIcon,
            IsVisible = true,
            Menu = new NativeMenu
            {
                Items =
                {
                    _openMenuItem,
                    new NativeMenuItemSeparator(),
                    _exitMenuItem
                }
            }
        };

        _trayIcon.Clicked += (s, e) => RestoreWindow();

        var trayIcons = new TrayIcons { _trayIcon };
        TrayIcon.SetIcons(app, trayIcons);

        UpdateTrayText();
    }

    private void RemoveTrayIcon()
    {
        var app = Application.Current;
        if (app is null) return;

        if (_trayIcon is not null)
        {
            _trayIcon.IsVisible = false;
            var trayIcons = TrayIcon.GetIcons(app);
            trayIcons?.Remove(_trayIcon);
            _trayIcon = null;
        }
    }

    private void UpdateTrayText()
    {
        if (_trayIcon is null || DataContext is not MainWindowViewModel viewModel) return;

        _trayIcon.ToolTipText = viewModel.L("TrayToolTip", "CXTracer");

        if (_openMenuItem is not null)
        {
            _openMenuItem.Header = viewModel.L("TrayOpen", "Open CXTracer");
        }

        if (_exitMenuItem is not null)
        {
            _exitMenuItem.Header = viewModel.L("TrayExit", "Exit");
        }
    }
}
