# App Shell & Hosts

This document defines the required shell setup for Avalonia desktop apps that use SukiUI.

## App Startup

Initialize `SukiTheme` in `App.axaml` and set a theme color explicitly.

```xml
<Application
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:suki="clr-namespace:SukiUI;assembly=SukiUI"
    x:Class="MyApp.App">
    <Application.Styles>
        <suki:SukiTheme ThemeColor="Blue" Locale="en-US" />
    </Application.Styles>
</Application>
```

## Required Rules

- `ThemeColor` must be set. SukiUI documents that windows and controls can become transparent if it is missing.
- If the project started from the default Avalonia template, remove `Avalonia.Themes.Fluent` once SukiUI becomes the active theme layer.
- Set `Locale` only when the app needs non-English built-in control text.

## Main Window

The application shell must inherit from `SukiWindow` in both AXAML and code.

```xml
<suki:SukiWindow
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:suki="clr-namespace:SukiUI.Controls;assembly=SukiUI"
    x:Class="MyApp.Views.MainWindow">

    <suki:SukiWindow.Hosts>
        <suki:SukiDialogHost Manager="{Binding DialogManager}" />
        <suki:SukiToastHost Manager="{Binding ToastManager}" />
    </suki:SukiWindow.Hosts>

    <suki:SukiSideMenu>
        <!-- shell content -->
    </suki:SukiSideMenu>
</suki:SukiWindow>
```

```csharp
using SukiUI.Controls;

namespace MyApp.Views;

public partial class MainWindow : SukiWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
```

## Host Placement

`SukiWindow.Hosts` is valid only on `SukiWindow`. Do not declare dialog or toast hosts inside page `UserControl`s.

Use hosts for:

- `SukiDialogHost` for modal interactions
- `SukiToastHost` for queued transient feedback
- other overlay content that must render above the full window chrome

## Shell Composition

Preferred shell order:

1. `App.axaml` registers `SukiTheme`
2. `MainWindow` derives from `SukiWindow`
3. `SukiWindow.Hosts` declares dialog and toast hosts
4. shell content uses `SukiSideMenu`, tabs, or a page presenter

## Forbidden Patterns

- Plain `Window` as the main desktop shell
- Hosts declared inside reusable pages
- Static global window instances to show dialogs or toasts
- Missing `ThemeColor` in `SukiTheme`
