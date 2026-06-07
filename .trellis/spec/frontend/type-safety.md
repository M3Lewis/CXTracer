# Type Safety and Bindings

The project enables nullable reference types and Avalonia compiled bindings by default.

## Project Settings

`CXTracer.csproj` includes:

```xml
<Nullable>enable</Nullable>
<LangVersion>latest</LangVersion>
<ImplicitUsings>enable</ImplicitUsings>
<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
```

## AXAML Binding Rules

Set `x:DataType` on bound views and templates.

Current examples:

- `MainWindow.axaml` sets `x:DataType="vm:MainWindowViewModel"`.
- session item templates set `x:DataType="models:SessionInfo"`.
- event item templates set `x:DataType="models:DisplayEvent"`.

## Model and ViewModel Rules

- Use `required` init properties for model values that must exist.
- Use nullable annotations for optional values such as `SessionInfo? SelectedSession`.
- Keep optional timestamp as `DateTimeOffset?`.
- Prefer concrete typed collections over `object` bags.

## Command Rules

- Use `[RelayCommand]` from CommunityToolkit.Mvvm.
- Async commands should return `Task`.
- Keep generated command names predictable: `RefreshAsync` becomes `RefreshCommand`, `OpenDefaultRoot` becomes `OpenDefaultRootCommand`.

## Parser Rules

Codex transcript JSONL is not a stable public API. Parser code should use `System.Text.Json`, tolerate missing keys, and classify with explicit enums rather than dynamic objects.

## Avoid

- Disabling compiled bindings to make an AXAML error disappear.
- Using `dynamic` for transcript parsing.
- Exposing mutable service internals directly to AXAML.
- Ignoring nullable warnings in new code.
