# Design: Message Detail Popup

## Architecture

The popup is implemented as an **overlay panel** inside the existing `MainWindow.axaml`, not a separate Window or SukiUI Dialog. This avoids adding dialog infrastructure and keeps the overlay lifecycle simple.

### Components

```
MainWindow.axaml
  └── Grid (root)
      ├── ... existing content ...
      └── Panel (overlay, spans all rows/columns)
          ├── Border (backdrop: semi-transparent black, click-to-dismiss)
          └── Border (content panel: centered, scrollable, styled per event kind)
```

### State

- `MainWindowViewModel.DetailPopupEvent` (`DisplayEvent?`) — the event being shown. `null` = popup closed.
- `MainWindowViewModel.IsDetailPopupOpen` (computed `bool`) — drives overlay visibility.
- `MainWindowViewModel.ShowDetailPopupCommand` / `CloseDetailPopupCommand` — RelayCommands.

### Data flow

1. **Open**: Card `Border` has `PointerPressed` handler → calls `viewModel.ShowDetailPopup(displayEvent)`
2. **Close**: Backdrop click / Escape key / × button → calls `viewModel.CloseDetailPopup()`
3. **Keyboard guard**: `Window_KeyDown` checks `IsDetailPopupOpen` before processing Up/Down/Left/Right.

### Visual structure of the popup panel

```
┌─────────────────────────────────────────────────┐
│  [Role Badge]  [Timestamp]              [× btn] │
├─────────────────────────────────────────────────┤
│  Title (bold, if present)                       │
│                                                 │
│  Text content (scrollable, wrap, readable font) │
│                                                 │
│  ▶ Raw JSON (collapsible Expander)              │
│    { "type": "...", ... }                       │
└─────────────────────────────────────────────────┘
```

### Key decisions

- **No new AXAML file**: The overlay lives directly in `MainWindow.axaml` as a `Panel` layer on top of the root `Grid`. Simpler than a UserControl or Window.
- **PointerPressed on card Border, not ListBox selection**: Avoids interfering with ListBox selection mechanics.
- **Escape intercept**: Added at the top of `Window_KeyDown`, before all other key handling.
