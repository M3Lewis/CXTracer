# App Shell

CXTracer uses the standard Avalonia desktop lifetime with a SukiUI main window.

## Startup

`Program.cs` builds the app with platform detection, Inter font, and trace logging:

```csharp
public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
```

`App.axaml.cs` manually constructs services and sets `desktop.MainWindow`.

## Theme Setup

`App.axaml` registers SukiUI:

```xml
<Application.Styles>
    <suki:SukiTheme Locale="zh-CN" ThemeColor="Orange" />
</Application.Styles>
```

Keep `ThemeColor` explicit. The app currently requests the light theme variant.

## Main Window

`Views/MainWindow.axaml` must remain a Suki window:

```xml
<suki:SukiWindow
    x:Class="CXTracer.Views.MainWindow"
    x:DataType="vm:MainWindowViewModel"
    Width="1380"
    Height="860"
    MinWidth="1080"
    MinHeight="640"
    Title="CXTracer"
    BackgroundStyle="Flat">
```

`Views/MainWindow.axaml.cs` derives from `SukiWindow`.

## Hosts

The current app does not use `SukiDialogHost` or `SukiToastHost`. User feedback is inline through `StatusMessage` and the footer progress bar.

If dialogs or toasts are added later, declare hosts only under `SukiWindow.Hosts` in `MainWindow.axaml` and expose managers from the shell ViewModel.

## Avoid

- Replacing `SukiWindow` with plain `Window`.
- Creating additional main windows for feature panels.
- Adding dialog/toast hosts inside reusable controls or item templates.
