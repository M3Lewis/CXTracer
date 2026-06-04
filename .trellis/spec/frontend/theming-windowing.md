# Theming and Windowing

The current visual design is a light, dense desktop tool built on SukiUI.

## Current Theme

`App.axaml` sets:

```xml
<Application RequestedThemeVariant="Light">
    <Application.Styles>
        <suki:SukiTheme Locale="zh-CN" ThemeColor="Orange" />
    </Application.Styles>
    <Application.Resources>
        <Color x:Key="SystemAccentColor">#E97924</Color>
        <SolidColorBrush x:Key="SystemAccentColorBrush" Color="#E97924" />
    </Application.Resources>
</Application>
```

`MainWindow.axaml` sets `BackgroundStyle="Flat"` and fixed startup dimensions:

- `Width="1380"`
- `Height="860"`
- `MinWidth="1080"`
- `MinHeight="640"`

## Current Palette Reality

The UI currently uses literal light colors for surfaces, borders, event cards, and badges. Examples include:

- main surface `#FBF7EF`
- panel surface `#FFFDF8`
- border `#E7DCCB`
- event-kind colors on `DisplayEvent`

Document this as current reality. Do not pretend the app is fully dynamic-theme ready.

## Change Rules

- Preserve readability for long transcript text.
- Keep `ThemeColor="Orange"` unless a visual refresh intentionally changes the app accent.
- Keep Fluent accent resources aligned with the orange app accent. See [Fluent Accent Resource Coverage](./atoms/fluent-accent-resource-coverage.md).
- Keep `BackgroundStyle="Flat"` unless there is a clear UX reason to change it.
- If adding dark mode, first replace hardcoded surface and event colors with theme-aware resources.
- Keep event-kind color mapping centralized on `DisplayEvent` or a deliberate presentation styling layer.

## Window Rules

- Main shell stays `SukiWindow`.
- Code-behind constructor should only call `InitializeComponent()`.
- Title, minimum size, and shell background belong in AXAML.

## Avoid

- Mixing another Avalonia theme package into the active app theme.
- Adding animated/decorative backgrounds to the transcript workspace.
- Hardcoding more colors in scattered AXAML fragments without considering centralization.
