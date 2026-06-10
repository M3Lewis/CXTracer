# Enhance UI Features - PRD

## Goal

Enhance the CXTracer desktop UI by supporting image rendering within conversation/execution cards, providing collapsible structures for tool calls/results, and syntax-coloring diffs with additions/deletions counts.

## Confirmed Facts

1. CXTracer is an Avalonia-based cross-platform desktop application using C# and .NET 8.
2. The UI is built using the SukiUI framework with orange/warm theme styling.
3. Currently, all event texts are displayed in standard flat `TextBlock` elements, which only support plain text rendering and basic keyword highlighting.
4. Codex logs include tool calls (`custom_tool_call`), tool results (`custom_tool_call_output`), commands, outputs, and diffs.
5. In addition to local image paths, some logs may reference images using base64 data URIs.

## Requirements

### REQ-1: Render Images in Event Cards
- **REQ-1.1**: Detect image references in event text. This includes:
  - Markdown image syntax: `![caption](image_path)`
  - HTML image tags: `<img src="image_path" />`
  - Raw local absolute/relative file paths ending in `.png`, `.jpg`, `.jpeg`, `.webp`, `.gif`.
  - Base64 data URIs (e.g., `data:image/webp;base64,...`).
- **REQ-1.2**: Resolve relative image paths using the active session file's directory and the user's home directory (`~`).
- **REQ-1.3**: Load and render the image using an Avalonia `Image` control inside a neat rounded border below the text.
- **REQ-1.4**: Implement robust exception handling (e.g., file not found, invalid format) in the image loader to prevent application crashes.

### REQ-2: Distinguish and Fold Tool Calls & Tool Results
- **REQ-2.1**: Split the existing `EventKind.Tool` into `EventKind.ToolCall` and `EventKind.ToolResult`.
- **REQ-2.2**: Map `custom_tool_call` payloads to `ToolCall` and `custom_tool_call_output` to `ToolResult`.
- **REQ-2.3**: Update colors and badges for `ToolCall` ("Tool Call") and `ToolResult` ("Tool Output") to distinguish them clearly.
- **REQ-2.4**: Implement an expandable/collapsible toggle mechanism for tool calls, tool results, diffs, command outputs, or any card with text longer than 200 characters.
- **REQ-2.5**: Show a 3-line preview with `...` when collapsed, and the full content when expanded.
- **REQ-2.6**: Add an interactive toggle button in the card header for all expandable cards.
- **REQ-2.7**: Hybrid Default Expansion state:
  - Left column events (User, Assistant, Final): Always start **expanded**.
  - Right column events (Diff, ToolCall, ToolResult, Command, CommandOutput): Start **collapsed** by default if the content is longer than 150 characters OR contains more than 3 lines; otherwise, start **expanded**.
- **REQ-2.8**: Persistent Settings Option for Default Expansion: Provide a setting in the Settings Window to allow the user to toggle whether all expandable messages should be expanded by default. If enabled, this overrides the default hybrid collapsed state, and dynamically expands all currently loaded and future events.

### REQ-3: Syntax Color Diffs and Display Line Counts
- **REQ-3.1**: Compute diff additions and deletions counts for diff events (excluding metadata lines starting with `+++` and `---`).
- **REQ-3.2**: Display a badge in the top-right corner of the diff card indicating additions and deletions counts (e.g., `+15` in green, `-3` in red).
- **REQ-3.3**: Color code individual lines inside the diff card:
  - Additions (starting with `+`): light green background, dark green text.
  - Deletions (starting with `-`): light red background, dark red text.
  - Meta/Hunk headers (starting with `@@`, `diff --git`, etc.): light blue/gray background, dark blue/gray text.
  - Context lines: standard text, transparent background.
- **REQ-3.4**: Render the colored line-by-line diff only when the diff card is expanded (for high performance).

## Acceptance Criteria

- [ ] Image references in event text (local paths or base64) render visually in the card.
- [ ] Tool calls and outputs have distinct badge labels and styling.
- [ ] Cards with long texts, diffs, or tools display a toggle button in the header and collapse/expand correctly.
- [ ] A settings option to toggle "Expand messages by default" is available in the Settings window, saves/loads correctly, and dynamically applies to loaded events.
- [ ] Diff cards display the exact additions/deletions line count in the top-right.
- [ ] Diff lines are colored red and green in expanded view.
- [ ] All new components are fully compatible with .NET Native AOT compilation (no runtime reflection).

## Out of Scope

- Inline markdown rendering for general bold/italic text.
- Live editing of diffs or logs.

## Open Questions

None.
