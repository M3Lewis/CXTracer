# Implementation Plan

## Preconditions

- Review `prd.md`, `design.md`, and `research/avalonia-12-sukiui-7.md`.
- Load frontend specs through `trellis-before-dev` before editing code.
- Confirmed Avalonia patch target: `12.0.4`.

## Checklist

1. Reconfirm package availability:
   - SukiUI `7.0.1`
   - Avalonia `12.0.4`
2. Update `src/CodexLens/CodexLens.csproj`:
   - align `Avalonia`, `Avalonia.Desktop`, and `Avalonia.Fonts.Inter` to the selected Avalonia 12 patch
   - set `SukiUI` to `7.0.1`
   - remove `Avalonia.Diagnostics` unless dev tools are intentionally preserved
3. Run restore:
   - `dotnet restore .\CodexLens.sln`
4. Fix compile errors in the smallest scope:
   - package API changes
   - SukiUI namespace/property changes
   - AXAML selector/template changes
   - visual-tree API changes
5. Keep project contracts intact:
   - no disabling compiled bindings
   - no replacing `SukiWindow` with `Window`
   - no replacing the theme stack
   - no moving view-only scroll logic into ViewModels
6. Update documentation version references, especially README stack notes.
7. Run validation:
   - `dotnet build .\CodexLens.sln --configuration Debug --no-restore`
   - `dotnet build .\CodexLens.sln --configuration Release --no-restore`
   - `dotnet format .\CodexLens.sln --verify-no-changes`
   - `dotnet test .\CodexLens.sln --configuration Release --no-build` if test projects exist or if the command is meaningful for this solution
8. Run or document manual smoke verification:
   - app launch
   - main window rendering and theme styles
   - session list display
   - session selection
   - search/filter controls
   - settings window open/close
   - shortcut capture
   - transcript up/down buttons
   - keyboard pane navigation
   - raw events expander
   - busy/status indicators

## Review Gates

- Build success alone is not enough. Each PRD acceptance criterion needs evidence.
- If restore fails because SukiUI 7.0.1 depends on a different Avalonia version range, record the exact dependency conflict before editing versions.
- If runtime launch fails due to missing Suki styles/resources, inspect `App.axaml`, SukiUI package resource paths, and `SukiTheme` setup before changing controls.
- If AXAML compiled bindings fail, fix `x:DataType` or binding expressions rather than disabling compiled bindings.

## Rollback Points

- After package file edit but before compatibility fixes.
- After restore succeeds but before broad AXAML edits.
- After build succeeds but before documentation/manual verification changes.
