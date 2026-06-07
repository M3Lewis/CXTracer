# Message Detail Popup on Click

## Goal

Allow users to click on any message card in the Conversation or Execution pane to open a centered floating overlay that displays the full message content in an enlarged, scrollable view.

## User Value

Long messages (especially Assistant responses, Tool call payloads, and Command outputs) are compressed in the narrow pane column. Users need a way to read the full content without losing their navigation position in the transcript.

## Confirmed Facts

- **Message data model**: `DisplayEvent` contains `Title`, `Text`, `RawJson`, `Kind`, `RoleLabel`, `TimeText`, and visual properties (`CardBackground`, `CardBorder`, etc.).
- **Card template**: Both panes use identical `DataTemplate` structure: `Border` > `StackPanel` > role badge, timestamp, title, text `TextBlock`s. Text uses `TextWrapping="Wrap"`.
- **No existing dialog/popup infrastructure**: Zero dialogs, popups, or overlays exist. Spec says to use `SukiDialogHost` if dialogs are added, but for this feature a custom overlay panel is simpler and more controllable.
- **ListBox interaction**: `SelectionMode="Toggle"`, `Focusable="False"`. Custom ListBoxItem template with bare `ContentPresenter` — no visible selection chrome. Single click does not produce any visible side effect.
- **Keyboard navigation**: Handled at Window level via `KeyDown`. The popup must not break existing Up/Down/Escape key handling.

## Requirements

### REQ-1: Single-click to open detail popup

**AC-1.1**: Clicking on any message card in Conversation or Execution pane opens the detail overlay.
**AC-1.2**: The click is handled on the card `Border`, not the ListBox selection.

### REQ-2: Centered floating overlay with backdrop

**AC-2.1**: A semi-transparent dark backdrop covers the entire window.
**AC-2.2**: A centered panel (≈70–80% window width, ≈80% height) displays the message content.
**AC-2.3**: The panel uses the same card colors (`CardBackground`, `RoleBadgeBackground`, etc.) as the source card.

### REQ-3: Content layout — Text + collapsible RawJson

**AC-3.1**: The overlay shows the role badge, timestamp, and title at the top.
**AC-3.2**: The main body is the `Text` field in a scrollable area with comfortable reading typography.
**AC-3.3**: Below the text, an `Expander` labelled "Raw JSON" shows the `RawJson` content in monospace font when expanded.

### REQ-4: Dismiss behavior

**AC-4.1**: Clicking the backdrop (outside the panel) closes the overlay.
**AC-4.2**: Pressing Escape closes the overlay.
**AC-4.3**: A close button (×) in the panel header closes the overlay.
**AC-4.4**: While the overlay is open, Up/Down arrow keys do NOT navigate the transcript.

## Out of Scope

- Copy-to-clipboard button (follow-up)
- Markdown or syntax-highlighted rendering
- Editing message content
- Slide-in side panel or SukiUI dialog integration
