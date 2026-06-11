# Split Large Files

## Goal

Reduce file size of all `.cs` and `.axaml` files exceeding 500 lines to improve maintainability, readability, and reduce merge conflicts. Follow the file-splitter skill conventions for C#/Avalonia/CommunityToolkit.Mvvm projects.

## Confirmed Facts

- **Two files exceed 500 lines** (scanned via Python script across `src/`):
  - `MainWindowViewModel.cs` — **1412 lines** (ViewModel, partial class, CommunityToolkit.Mvvm)
  - `MainWindow.axaml` — **1151 lines** (Avalonia XAML View)
- The View code-behind is **already split** into partials:
  - `MainWindow.axaml.cs` (68 lines) — base, ctor, lifecycle
  - `MainWindow.Input.cs` (171 lines) — pointer/keyboard handlers
  - `MainWindow.Navigation.cs` (283 lines) — scroll/navigation
  - `MainWindow.Tray.cs` (unknown, small) — tray icon logic
- `FilterOptionItem` class is **embedded** in `MainWindowViewModel.cs` (lines 21–35) — should be its own file.
- The event card `DataTemplate` is **duplicated nearly verbatim** between the Conversation pane (lines 381–573) and Execution pane (lines 656–851) in `MainWindow.axaml` — ~190 lines each.
- Project uses `CommunityToolkit.Mvvm` with `[ObservableProperty]`, `[RelayCommand]`, partial classes.
- Git status: clean working tree (only untracked task files).

## Requirements

### R1: Split `MainWindowViewModel.cs` into partial classes

Decompose by concern into ≤500 lines per file:

| File | Responsibility |
|---|---|
| `MainWindowViewModel.cs` | Fields, collections, ctor, lifecycle, Dispose, computed properties |
| `MainWindowViewModel.Properties.cs` | `[ObservableProperty]` fields + all `partial void On*Changed` handlers |
| `MainWindowViewModel.Commands.cs` | `[RelayCommand]` methods, sync shortcut capture/confirm logic |
| `MainWindowViewModel.Sessions.cs` | Session loading, file-change handling, UpsertSession, enrichment |
| `MainWindowViewModel.Filtering.cs` | Event/session filtering, PassesFilter, PopulateVisibleEvents, ApplyFilter |
| `MainWindowViewModel.Navigation.cs` | Transcript navigation (GetSynchronizedNavigationTarget, companion, anchor) |
| `MainWindowViewModel.Settings.cs` | LoadSettings, SaveSettings, localization (L, LF, ApplyLanguage) |

Extract `FilterOptionItem` to its own file: `ViewModels/FilterOptionItem.cs`.

### R2: Deduplicate and reduce `MainWindow.axaml`

- Extract the duplicated event card `DataTemplate` into a shared XAML resource (e.g., `<DataTemplate x:Key="EventCardTemplate">`) referenced by both Conversation and Execution ListBoxes.
- Extract the detail popup overlay (lines 932–1071) into a UserControl `DetailPopupOverlay.axaml` if it has standalone logic, or keep inline if it's purely data-bound to the parent VM.
- Extract the image viewer overlay (lines 1073–1149) into a UserControl `ImageViewerOverlay.axaml` or keep inline.
- Goal: reduce `MainWindow.axaml` to ≤700 lines.

### R3: Preserve behavior

- All existing functionality must work identically after the split.
- No new warnings or build errors introduced.
- No namespace changes unless fixing an existing inconsistency.

## Acceptance Criteria

- [ ] AC1: No `.cs` file in `src/` exceeds 500 lines (excluding generated `.g.cs`).
- [ ] AC2: `MainWindow.axaml` is reduced to ≤700 lines.
- [ ] AC3: `dotnet build` succeeds with zero errors.
- [ ] AC4: Application launches and displays sessions/events identically to before the split.
- [ ] AC5: `FilterOptionItem` is in its own file under `ViewModels/`.

## Out of Scope

- Functional changes, new features, or bug fixes.
- Refactoring services (`SessionScanner`, `SessionReader`, etc.).
- Splitting `DisplayEvent.cs` or other model files (currently under 500 lines).
- Introducing DI or architectural changes.
