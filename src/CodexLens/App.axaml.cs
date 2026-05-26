using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CodexLens.Services;
using CodexLens.ViewModels;
using CodexLens.Views;

namespace CodexLens;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var parser = new CodexEventParser();
            var scanner = new SessionScanner(parser);
            var reader = new SessionReader(parser);
            var watcher = new SessionWatcher();
            var settings = new AppSettingsService();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(scanner, reader, watcher, settings)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
