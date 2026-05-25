# Theming & Windowing

SukiUI should own the visual language of the desktop application. Prefer its built-in theme APIs and resources over custom styling.

## Base Theme Switching

Use `SukiTheme.GetInstance()` to switch light and dark mode.

```csharp
var theme = SukiTheme.GetInstance();

theme.ChangeBaseTheme(ThemeVariant.Dark);
theme.ChangeBaseTheme(ThemeVariant.Light);
theme.SwitchBaseTheme();
```

You may subscribe to `OnBaseThemeChanged` when the application needs to react to theme changes.

## Color Theme Switching

Use Suki color themes instead of hardcoded accent brushes.

```csharp
var theme = SukiTheme.GetInstance();

theme.ChangeColorTheme(SukiColor.Red);
theme.SwitchColorTheme();

var customTheme = new SukiColorTheme("Purple", Colors.Purple, Colors.DarkBlue);
theme.AddColorTheme(customTheme);
theme.ChangeColorTheme(customTheme);
```

## Window Background Styles

`SukiWindow` supports three built-in background modes:

- `Flat`: best default for dense, work-focused tools
- `Gradient`: use when the app needs more visual depth
- `Bubble`: use selectively; it is the most decorative option

```xml
<suki:SukiWindow BackgroundStyle="Flat">
    <!-- content -->
</suki:SukiWindow>
```

## Performance Rules

SukiUI background animation is visually strong but not free.

- Enable background animation only when it materially improves the experience.
- Avoid animated backgrounds on data-heavy screens.
- Prefer `Flat` backgrounds for long-running dashboards, editors, and productivity tools.

## Title Bar and Shell Features

Use built-in `SukiWindow` slots before creating custom chrome.

- `LogoContent` for app branding
- `MenuItems` for window-level actions
- `RightWindowTitleBarControls` for compact utility controls

## Resource Usage

Prefer Suki theme resources and dynamic resources over literal color values.

Good:

```xml
<Border Background="{DynamicResource SukiBackgroundBrush}" />
```

Avoid:

```xml
<Border Background="#1E1E1E" />
```

## Forbidden Patterns

- Hardcoded light/dark colors for standard surfaces
- Custom title bars when `SukiWindow` already provides the needed chrome
- Background animation enabled by default across the whole product
- Shader-based backgrounds without a clear design or performance reason
