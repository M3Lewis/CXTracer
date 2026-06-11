# Product Requirements Document

## Goal and User Value
The `DisplayEvent.cs` model has grown significantly with additions of collapsible states, diff block parsing, WSL path resolution, and base64 image placeholders. To keep the codebase maintainable and readable, we will reorganize and clean up `DisplayEvent.cs` by splitting it into partial classes grouped by concerns (styling, image extraction/resolution, and core properties).

## Confirmed Facts
- `DisplayEvent` is a sealed partial class inheriting from `ObservableObject`.
- It currently holds properties, static regex patterns for Markdown/HTML parsing, thread-safe color brushes, WSL/Unix path resolvers, base64 extraction logic, and diff lines parse helper.
- The file has reached over 760 lines.

## Requirements
1. **Partial Class Separation**: Split `DisplayEvent.cs` into:
   - `DisplayEvent.cs`: Core properties, initialization flow, expansion state, and diff line parsing.
   - `DisplayEvent.Styling.cs`: Visual properties, thread-safe background/border brushes, and role labels.
   - `DisplayEvent.Images.cs`: Regex properties, local/json image path extraction, and path/placeholder resolution.
2. **Zero Functional Changes**: Ensure no logic changes are introduced to the properties or methods.
3. **Clean Build**: The project must build successfully after reorganization.

## Acceptance Criteria
- All properties and helper functions are preserved.
- The class is correctly split into three partial files.
- The project builds cleanly with 0 errors.
