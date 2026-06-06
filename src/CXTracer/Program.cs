using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Fonts.Inter;
using System;
using Avalonia.Controls;

namespace CXTracer;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
