# Avalonia + SukiUI Quality Checklist

Use this checklist during implementation and review.

## Shell Setup

- `App.axaml` registers `SukiTheme`
- `ThemeColor` is explicitly set
- the main desktop shell derives from `SukiWindow`
- dialog and toast hosts are declared only in `SukiWindow.Hosts`

## MVVM

- ViewModels expose state and commands, not visual tree references
- code-behind is empty or limited to view-only glue
- repeated interaction logic is moved to behaviors or attached properties

## Controls and Navigation

- `SukiSideMenuItem` always includes `PageContent`
- page navigation state lives in the shell ViewModel
- SukiUI controls are preferred over one-off styled replacements

## Bindings and Commands

- views use compiled bindings with `x:DataType`
- asynchronous actions use `Task`-returning relay commands
- converters stay lightweight and deterministic

## Theming

- no hardcoded standard surface colors
- theme changes use `SukiTheme.GetInstance()`
- background style choice matches the product's density and performance needs

## Performance

- background animation is disabled unless it adds clear value
- no heavy logic in converters or UI thread callbacks
- long-running operations surface progress or busy state

## Forbidden Patterns

- `async void` commands
- static window references for feature logic
- hosts placed inside page controls
- plain `Window` used as the main app shell
- missing `ThemeColor` on `SukiTheme`
