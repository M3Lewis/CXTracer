# Dependency Composition

The project currently uses manual composition, not `Microsoft.Extensions.DependencyInjection`.

## Current Pattern

`App.axaml.cs` creates the service graph when the classic desktop lifetime is available:

```csharp
var parser = new CodexEventParser();
var scanner = new SessionScanner(parser);
var reader = new SessionReader(parser);
var watcher = new SessionWatcher();
desktop.MainWindow = new MainWindow
{
    DataContext = new MainWindowViewModel(scanner, reader, watcher)
};
```

This is acceptable for the current app because there is one window, one ViewModel, and four concrete services.

## Rules

- Keep dependencies explicit through constructors.
- Do not introduce a static service locator.
- Do not pass `IServiceProvider` into ViewModels or services.
- Add a DI container only when the composition graph becomes large enough that manual construction hurts clarity.

## Lifetime Notes

- `CodexEventParser` is stateless and can be shared.
- `SessionScanner` depends on the parser and is stateless across scans.
- `SessionReader` owns tail state and should be shared for the active window.
- `SessionWatcher` owns a `FileSystemWatcher` and must be disposed by the ViewModel.

## If DI Is Added Later

Use one composition root and preserve the same lifetimes:

- parser and scanner: singleton or shared service
- reader: singleton for the active app session
- watcher: singleton or owned by the shell ViewModel
- ViewModels: constructed with explicit service dependencies
