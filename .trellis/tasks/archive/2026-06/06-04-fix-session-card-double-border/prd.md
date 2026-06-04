# Fix session card double border

## Goal

Remove the inner border from the selected session card and make the remaining outer border as readable as the previous inner border.

## Requirements

- In the Codex sessions list, a selected session item must show only one item-level border.
- The remaining border must be the outer selected-item border, not an inner card border.
- The outer selected-item border must be as readable as the previous inner card border in the screenshot.
- Do not change transcript event card borders, status badge borders, toolbar borders, or the outer left-pane container border.

## Acceptance Criteria

- [ ] The selected session row no longer renders a nested card border inside the selected-item border.
- [ ] The selected session row keeps a visible single outer border.
- [ ] The visible outer border uses the same readable neutral border color as the former inner card border.
- [ ] `dotnet build .\CodexLens.sln` succeeds.

## Notes

- Screenshot target: the selected item in the left `Codex sessions` list, not the session-list container.
- Lightweight task; PRD-only planning is sufficient.
