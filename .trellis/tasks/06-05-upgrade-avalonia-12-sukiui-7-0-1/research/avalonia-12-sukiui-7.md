# Avalonia 12 and SukiUI 7 Research

Research date: 2026-06-05

## Sources

- Avalonia 12 breaking changes: https://docs.avaloniaui.net/docs/avalonia12-breaking-changes
- Avalonia 12 release blog: https://avaloniaui.net/blog/avalonia-12/
- Avalonia NuGet package: https://www.nuget.org/packages/Avalonia
- Suki NuGet profile: https://www.nuget.org/profiles/Suki
- SukiUI NuGet package: https://www.nuget.org/packages/SukiUI
- SukiUI installation docs: https://kikipoulet.github.io/SukiUI/documentation/getting-started/installation.html

## External Findings

- Avalonia 12 supports .NET 8 and later for desktop apps. The docs recommend .NET 10, but this app is currently desktop-only on `net8.0`, so changing target framework is not required for the requested upgrade.
- Avalonia guidance says to upgrade all Avalonia package references to the latest 12 patch version. NuGet evidence gathered on 2026-06-05 listed `12.0.4` as the latest stable Avalonia 12 package, and the user selected `12.0.4` for this task.
- `Avalonia.Diagnostics` was removed in Avalonia 12. The documented replacement is `AvaloniaUI.DiagnosticsSupport`, but this project does not currently call the dev tools attach APIs.
- Avalonia 12 enables compiled bindings by default. This project already explicitly enables compiled bindings.
- `UsePlatformDetect()` should use HarfBuzz by default. The project does not explicitly call `UseSkia()`, so the documented "No text shaping system configured" path is not expected.
- Suki NuGet evidence lists SukiUI packages as targeting .NET 8.0 compatible or higher, with SukiUI `7.0.1` published on 2026-05-19. The public SukiUI installation docs still show older example versions, so NuGet should be treated as the package-version source of truth.

## Local Evidence

- Project file: `src/CodexLens/CodexLens.csproj`
  - `TargetFramework` is `net8.0`.
  - Avalonia packages are `11.3.14`.
  - SukiUI is `6.1.1`.
  - `Avalonia.Diagnostics` is referenced only for Debug.
- Theme file: `src/CodexLens/App.axaml`
  - registers `SukiTheme Locale="zh-CN" ThemeColor="Orange"`.
  - defines orange accent resources and project toolbar/check styles.
- Shell views:
  - `src/CodexLens/Views/MainWindow.axaml` is `suki:SukiWindow`, has `x:DataType`, uses `BackgroundStyle="Flat"`, and defines Suki/window-local styles.
  - `src/CodexLens/Views/SettingsWindow.axaml` is `suki:SukiWindow`, has `x:DataType`, and uses `BackgroundStyle="Flat"`.
- Code-behind:
  - `MainWindow.axaml.cs` derives from `SukiWindow` and uses `GetVisualDescendants`, `GetVisualAncestors`, `ContentPresenter`, `ScrollViewer.Offset`, and translated coordinates for view-only navigation.
  - `SettingsWindow.axaml.cs` derives from `SukiWindow`, disposes its ViewModel on close, and handles shortcut capture/close click.
- Static search did not find local usage of `IClipboard`, `DataObject`, `WindowState` styling, `FuncMultiValueConverter`, hand-written C# Avalonia bindings, `DispatcherTimer`, `Gestures.*`, focus event handlers, or custom rendering APIs.

## Planning Implications

- The migration is likely package and AXAML compatibility work rather than broad architecture work.
- The implementation should keep the app on `net8.0` and avoid bringing in .NET 10 as hidden scope.
- The highest-risk files are the project file, Suki theme setup, SukiWindow AXAML, and window code-behind visual-tree helpers.
- Manual runtime launch is required because package restore/build may not catch missing SukiUI styles or template resource changes.
