# Fix toolbar and selection theme colors

## Goal

Fix two visible theme regressions in the Codex Lens Avalonia UI:

1. Black selection/scroll/progress indicators shown in the screenshot and follow-up testing should use the app's orange accent theme instead of black.
2. Text toolbar buttons should keep their border visible on pointer hover. `Raw` already behaves correctly; `Settings`, `Clear search`, `Default`, and `Refresh` currently lose their border on hover.

The fix should restore consistent orange-accent visual feedback without changing the recently corrected toolbar button sizes.

## Confirmed Facts

- The app registers SukiUI with `ThemeColor="Orange"` in `src/CodexLens/App.axaml`.
- `.trellis/spec/frontend/theming-windowing.md` correctly documents the orange theme.
- `.trellis/spec/frontend/app-shell-hosts.md` and `.trellis/spec/frontend/index.md` still mention Blue and are stale for the current app.
- The relevant main window UI lives in `src/CodexLens/Views/MainWindow.axaml`.
- Existing toolbar styles include `Button.toolbarButton`, `ToggleButton.toolbarButton`, `Button.roundIconButton`, and active transcript pane border styles.
- A previous fix made `Raw` hover keep its border and corrected second-row toolbar button sizing with `VerticalAlignment="Center"`.

## Requirements

- Theme consistency:
  - Replace or override the black visual indicators highlighted in the screenshot so they render with the orange accent family used elsewhere in the app.
  - The selected-session check icon must not render black.
  - The footer busy progress indicator must not render black.
  - Do not introduce a new accent color family.
- Toolbar hover behavior:
  - `Settings`, `Clear search`, `Default`, and `Refresh` must keep a visible border while hovered.
  - `Raw` must keep its current correct hover behavior.
  - The fix must not change toolbar button width, height, padding, or vertical alignment compared with the current corrected layout.
- Scope safety:
  - Do not modify transcript pane arrow button behavior unless it is directly required for the black indicator fix.
  - Do not change transcript navigation behavior, session selection behavior, filtering, search, or raw-event visibility.
- Spec follow-up:
  - Correct stale frontend spec references that still say the app theme color is Blue.
  - Update or add a frontend atom only if the implementation reveals a durable pitfall not already captured.

## Acceptance Criteria

- [x] The selected-session check icon renders in the app's orange accent style.
- [x] The footer progress indicator renders in the app's orange accent style.
- [x] The transcript/raw scroll indicators render in the app's orange accent style.
- [x] Hovering `Settings` keeps its border visible.
- [x] Hovering `Clear search` keeps its border visible.
- [x] Hovering `Default` keeps its border visible.
- [x] Hovering `Refresh` keeps its border visible.
- [x] Hovering `Raw` still keeps its border visible.
- [x] The toolbar buttons keep the current corrected size and vertical placement.
- [x] Transcript pane arrow buttons still look and work as before unless intentionally changed for the black indicator issue.
- [x] `dotnet build CodexLens.sln --no-restore` passes, or an equivalent build verification is recorded if the running app locks the output exe.
- [x] Frontend specs no longer conflict about `ThemeColor="Orange"` vs `ThemeColor="Blue"`.

## Notes

- User supplied a screenshot on 2026-06-04 showing black indicators and confirming the affected toolbar buttons.
- Follow-up user testing confirmed the toolbar buttons are fixed, while the selected-session check icon and footer progress indicator still rendered black.
- Final user testing confirmed the toolbar buttons, selected-session check icon, footer progress indicator, and scroll indicators are fixed.
- This is a lightweight UI regression task. PRD-only planning is sufficient unless implementation inspection reveals a broader styling architecture change is needed.
