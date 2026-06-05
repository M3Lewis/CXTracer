# Implicit Rules Exploration

## Entry: 2026-06-05 - Reuse existing toolbar button style

### Trigger

The user corrected the implementation target after noticing that SettingsWindow received a second copy of the `toolbarButton` style instead of reusing the previously fixed main-window style.

### Observed Evidence

- `src/CodexLens/Views/MainWindow.axaml` already contained the proven `Button.toolbarButton` and `ToggleButton.toolbarButton` styles.
- The in-progress diff duplicated the `Button.toolbarButton` style inside `src/CodexLens/Views/SettingsWindow.axaml`.
- The user explicitly asked why the previous style could not be reused and why another `ToolbarButtonChrome` was being introduced.

### Inferred Hidden Rules

- Shared visual fixes should live in one shared style location when the same class and behavior are needed in multiple windows.
- `PART_ToolbarButtonChrome` is part of the existing template fix and can remain, but it must not be duplicated in separate window-local style blocks.
- SettingsWindow buttons should opt into the existing shared `toolbarButton` class rather than owning a second style definition.

### Action Guidance

- Move the existing `toolbarButton` styles from MainWindow-local styles to application-level styles.
- Remove the duplicate SettingsWindow-local `toolbarButton` style block.
- Keep SettingsWindow button instances classified with `Classes="toolbarButton"`.
- Keep unrelated MainWindow-only styles, such as `roundIconButton` and session list styles, local to MainWindow.

### Confidence

High. The user's correction directly names the duplication problem, and the source diff shows the duplicated selector/template block.

## Entry: 2026-06-05 - SukiWindow caption button hover diagnosis

### Trigger

The user reported that the main window minimize and maximize caption buttons do not show the circular hover background, while the close button does, and asked whether this is a SukiUI bug or an application bug.

### Observed Evidence

- `src/CodexLens/Views/MainWindow.axaml` uses the default `suki:SukiWindow` title bar and only sets shell properties such as `Title`, dimensions, `Background`, `KeyDown`, and `BackgroundStyle`.
- `src/CodexLens/Views/MainWindow.axaml` contains no selectors for `PART_MinimizeButton`, `PART_MaximizeButton`, `PART_CloseButton`, caption buttons, or Suki title bar controls.
- App-level styles added during this task target explicit classes such as `Button.toolbarButton` and `CheckBox.preferenceCheckBox`; SukiWindow caption buttons do not opt into those classes from application code.
- The SukiUI 6.1.1 assembly contains SukiWindow template parts named `PART_TitleBar`, `PART_FullScreenButton`, `PART_PinButton`, `PART_MinimizeButton`, `PART_MaximizeButton`, and `PART_CloseButton`.
- The NuGet package does not provide loose AXAML files in the installed package directory; the SukiWindow template is compiled into `SukiUI.dll`.

### Inferred Hidden Rules

- Caption button hover behavior is owned by SukiWindow's internal template unless the app deliberately overrides title bar part styles.
- Because close hover works while minimize/maximize hover does not, the issue is likely a SukiUI template-state asymmetry or platform-specific SukiWindow caption handling, not a direct app-level selector conflict.
- Any app workaround should target SukiWindow caption button parts explicitly and narrowly, and should not reuse `toolbarButton`, `roundIconButton`, or transcript control styles.

### Action Guidance

- Treat this as a separate title-bar diagnostic/fix from the current toolbar/settings checkbox task.
- If implementing a workaround, first inspect the live visual tree or SukiUI source for exact caption button classes/template children; avoid guessing selectors from unrelated buttons.
- Prefer a minimal SukiWindow-scoped caption-button style override over replacing the entire SukiWindow template.

### Confidence

Medium. Application code evidence is clear, but the exact SukiWindow AXAML template is compiled into the package and was not fully decompiled in this pass.
