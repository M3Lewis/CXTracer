# Fix search box focus border and text selection

## Goal

Resolve two issues in the search/input TextBoxes (`SessionSearchText`, `EventSearchText`, `RootPath`, etc.):
1. When a TextBox is focused/clicked, its border line disappears.
2. Text selection (via dragging, double-clicking, or Ctrl+A) is either visually invisible or broken, making it difficult for the user to delete multiple characters at once.

## Confirmed Facts

- SukiUI (v7.0.1) has custom TextBox templates and animation states. In light mode, the focused state border brush or thickness can resolve to values that result in the border line disappearing.
- Default text selection colors (SelectionBrush and SelectionForegroundBrush) in SukiUI can be low-contrast or transparent, rendering the selection highlight completely invisible to the user.
- Setting explicit `SelectionBrush`, `SelectionForegroundBrush`, and focused/pointerover border styles inside `App.axaml` will cascade and override SukiTheme defaults, restoring high-visibility border lines and text selection highlights.

## Requirements

1. **Keep Focus Border Visible**:
   - When any TextBox is focused, it must render a clearly visible, crisp accent border using the primary orange theme color (`#E97924`) with a thickness of `1.5`.
2. ** tactile Hover Border**:
   - When hovering over a TextBox, the border should turn secondary orange (`#F08A3C`) for tactile feedback.
3. **High-Contrast Text Selection**:
   - Active text selection inside any TextBox must have a highly visible selection background (using tertiary accent peach `#F6B27A`) and dark text (`#3D3445`) so the user can easily see selected characters before deleting or replacing them.

## Acceptance Criteria

- [ ] Focus border remains visible and turns primary orange (`#E97924`, 1.5px) when clicking/focusing any TextBox.
- [ ] Hover border turns secondary orange (`#F08A3C`) when hovering.
- [ ] Selecting text (via Ctrl+A, dragging, or double-clicking) displays a clear peach selection background.
- [ ] Pressing backspace/delete or typing after selecting text successfully deletes/replaces the entire selection.
