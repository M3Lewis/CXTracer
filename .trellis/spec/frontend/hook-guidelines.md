# Behaviors and View Logic

Avalonia has no React hooks. Use bindings, commands, attached properties, behaviors, or narrowly scoped code-behind depending on the job.

## Preferred Order

1. Binding to ViewModel state for data and control values.
2. `[RelayCommand]` for user actions that affect app state.
3. Converter or computed property for simple presentation formatting.
4. Attached property or behavior when the same view-only interaction is reused.
5. Code-behind only when visual tree access is necessary.

## Current Code-Behind Exception

`MainWindow.axaml.cs` scrolls to adjacent rendered message cards. This belongs in code-behind because it needs:

- named `ItemsControl` and `ScrollViewer` instances
- `GetVisualDescendants()`
- `ContentPresenter` containers
- translated coordinates inside the current viewport
- direct `ScrollViewer.Offset` updates

Keep this logic view-only. It must not call services, parse JSON, mutate transcript state, or know about selected sessions.

## When to Extract

Extract to a behavior or attached property only when the same scroll interaction is reused by another view. Until then, keeping it local to `MainWindow.axaml.cs` is clearer.

## Avoid

- Event handlers for refresh, filtering, file IO, or parser work.
- Behaviors that call domain/services directly.
- Converters with side effects or IO.
- Nested callbacks when a named private method would be clearer.
