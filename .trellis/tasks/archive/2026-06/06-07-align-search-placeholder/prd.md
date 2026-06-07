# PRD: Search Redesign and UX Improvement

## Goal
Redesign the search functionality by separating session filtering (left sidebar) and transcript message filtering (right panel), aligning the search box locations with their respective visual scopes.

## User Value
- Allows users to search and filter through hundreds of session files in the left sidebar by title or path.
- Provides a dedicated, localized event text search box in the main panel's toolbar, preventing UI-level confusion.

## Confirmed Facts
- The search text box is currently on the left side, but it filters events on the right side.
- There is a `ComboBox` for event type filtering on the right-hand panel's toolbar.
- The `SelectedSessionPath` label is displayed in the first column of the main panel's toolbar.

## Requirements
- **REQ-1 (Session Filter)**: The search box above the session list in the left sidebar must filter the session list (`Sessions`) by matching `DisplayTitle`, `DisplaySubtitle`, or `FilePath` case-insensitively.
- **REQ-2 (Event Search Box)**: Move the event text search box to the main panel's top toolbar, to the left of the `ComboBox` filter.
- **REQ-3 (Toolbar Layout)**: Adjust/compress the `SelectedSessionPath` layout column width to accommodate the new event search text box.
