# PRD: Format Raw Events Display

## Goal and User Value
Currently, the "Raw events" panel at the bottom of the main window displays each event as a single-line unformatted JSON string (`RawJson`). This makes it extremely difficult for developers to inspect model reasoning, token usage, plans, or raw payloads. 

This task will:
- Formulate a pretty-printed, indented JSON representation of each raw event's JSON log.
- Enhance the UI in the "Raw events" expander to render these formatted blocks cleanly, with distinct headers and code-card styling.

## Confirmed Facts
- **Data Model**: `DisplayEvent` contains `RawJson` (unmodified JSON string from the `.jsonl` file).
- **Source Serialization**: `AppJsonContext` is decorated with `[JsonSourceGenerationOptions(WriteIndented = true)]` and provides `AppJsonContext.Default.JsonElement`, making it Native AOT compliant and ready to format JSONElements.
- **Lazy formatting with memoization**: The JSON formatting is calculated lazily in the getter of `FormattedRawJson` to optimize memory usage, with local memoization to avoid redundant computations during scroll operations.
- **UI Control**: Currently, `RawEvents` are displayed in a `ScrollViewer` + `ItemsControl` inside a bottom `Expander` in `MainWindow.axaml` (lines 538–549).

## Requirements

### REQ-1: Format Raw JSON with Indentation
- **AC-1.1**: The `DisplayEvent` class must expose a read-only property `FormattedRawJson` (or similar).
- **AC-1.2**: It parses `RawJson` into a `JsonDocument` and serializes it using `AppJsonContext.Default.JsonElement` to produce formatted, indented JSON.
- **AC-1.3**: If parsing or serialization fails, it falls back to the original `RawJson` value.

### REQ-2: Structured Card UI for Raw Events
- **AC-2.1**: Instead of a wall of single-line text, each raw event in the bottom panel should be presented in its own styled container/card.
- **AC-2.2**: The container should show a header indicating the Line Number and TimeText (or Kind), helping developers relate raw events to the main transcript.
- **AC-2.3**: The formatted JSON should be rendered in a monospace block, respecting newlines (`\n`) and indents.

### REQ-3: Click to open detail popup
- **AC-3.1**: Clicking on a Raw Event card in the bottom panel must open the centered floating overlay (Detail Popup).
- **AC-3.2**: The click must be handled on the card `Border` via the `CardBorder_PointerPressed` event handler.
- **AC-3.3**: The popup's "Raw JSON" expander should bind to `DetailPopupEvent.FormattedRawJson` so it displays pretty-printed, indented JSON.

### REQ-4: Right-Click and Copy Behavior
- **AC-4.1**: Right-clicking a Raw Event card copies `evt.RawJson` (the unmodified original JSON line) to the system clipboard.
- **AC-4.2**: Right-clicking standard transcript cards continues to copy `evt.Text`.

### REQ-5: Text Layout & Parsing Fallback
- **AC-5.1**: The raw event text block must use `TextWrapping="NoWrap"` to preserve indentation structure, with horizontal scroll bar support.
- **AC-5.2**: If the JSON is incomplete or invalid (causing parsing to fail), it must gracefully fall back to displaying the raw unmodified string.

## Out of Scope
- Editing or modifying raw JSON in the UI.
