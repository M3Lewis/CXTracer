# Fix scroll arrow navigation

## Goal

Fix a bug where the down arrow buttons in both conversation and execution panes only navigate once, then stop responding.

## Requirements

- Fix the scroll arrow navigation in both the Conversation and Execution panes.
- Down arrow clicks must continue moving to the next visible message/card after the first successful move.
- Up arrow clicks must continue moving to the previous visible message/card.
- The fix must remain view-only: no transcript parsing, service calls, or application state should move into `MainWindow.axaml.cs`.
- Keep the current two-pane layout and existing button bindings/names.

## Acceptance Criteria

- [x] In the Conversation pane, repeated down arrow clicks move through successive visible conversation cards until the last card.
- [x] In the Conversation pane, repeated up arrow clicks move through successive previous conversation cards until the first card.
- [x] In the Execution pane, repeated down arrow clicks move through successive visible execution cards until the last card.
- [x] In the Execution pane, repeated up arrow clicks move through successive previous execution cards until the first card.
- [x] Clicking an arrow at the first/last card clamps safely and does not throw.
- [x] `dotnet build .\CodexLens.sln` passes.

## Notes

- Reported behavior: down arrow buttons only work once and then appear to stop responding; left and right panes behave the same.
- Likely area: `Views/MainWindow.axaml.cs` scroll target/anchor detection.
- Lightweight bugfix; PRD-only task.
