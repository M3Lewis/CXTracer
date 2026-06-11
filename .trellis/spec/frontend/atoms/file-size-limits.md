---
id: frontend.file-size-limits
type: architecture_decision
priority: must
applies_when:
  - modifying or creating C# code files
  - modifying or creating AXAML files
code_anchors:
  - src/CXTracer/ViewModels/MainWindowViewModel.cs
  - src/CXTracer/Views/MainWindow.axaml
verify:
  - no single C# source file exceeds 500 lines
  - no single AXAML view file exceeds 700 lines
source:
  kind: human_confirmed
  ref: task-06-11-split-large-files
last_checked: 2026-06-11
---

# Rule

To keep the codebase maintainable and prevent git merge conflicts, files must adhere to the following size limits:
1. **C# source files (`.cs`)**: Must not exceed 500 lines of code.
2. **Avalonia XAML files (`.axaml`)**: Must not exceed 700 lines of code.

If a file exceeds these limits, it must be refactored/split using partial classes or child controls.

# Why

Large view models and view classes accumulate multiple responsibilities, making them hard to read, maintain, and test, and increasing the risk of git merge conflicts. Keeping them split into highly cohesive fragments improves code organization and developer focus.

# View & ViewModel Separation Patterns

### MainWindowViewModel Split (Partial Classes)
Decompose `MainWindowViewModel` into separate files by concern:
- `MainWindowViewModel.cs`: Fields, collections, constructor, and lifecycle.
- `MainWindowViewModel.Properties.cs`: `[ObservableProperty]` definitions and their corresponding partial `On*Changed` property change handlers.
- `MainWindowViewModel.Commands.cs`: `[RelayCommand]` methods and command execution logic.
- `MainWindowViewModel.Sessions.cs`: Session loading, file change monitoring, and session enrichment.
- `MainWindowViewModel.Filtering.cs`: Event and session filtering, query matching, and visible collection populating.
- `MainWindowViewModel.Navigation.cs`: Scrolling anchors and synchronized navigation coordinate mapping.
- `MainWindowViewModel.Settings.cs`: Application settings loading, saving, and localization.

### MainWindow View Split (Partial Classes)
Decompose `MainWindow` view code-behind into separate files by concern:
- `MainWindow.axaml.cs`: Core window initialization, view model setup, and lifecycle events.
- `MainWindow.Input.cs`: Keyboard navigation events, keyboard shortcuts, and clipboard copying handlers.
- `MainWindow.Navigation.cs`: Scroll synchronization logic and coordinate calculations.
- `MainWindow.Tray.cs`: System tray icon creation, update, removal, and window-minimize interception.

### XAML Deduplication & Decomposition
- **Shared Templates**: Extract repetitive inline visual elements (such as `DataTemplate` for item lists) into static resources (e.g., within `<suki:SukiWindow.Resources>`) to be shared between controls. Use style `Classes` on parent containers to apply visual overrides.
- **Overlay UserControls**: Separate overlay panels and modals (such as `DetailPopupOverlay` and `ImageViewerOverlay`) into dedicated `UserControl` classes (each with its own `.axaml` and `.axaml.cs` code-behind) rather than keeping them inline.
