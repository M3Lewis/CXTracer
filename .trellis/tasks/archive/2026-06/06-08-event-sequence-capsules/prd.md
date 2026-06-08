# Event Sequence Capsules

## Goal

Add sequence number indicators (capsules) to the event cards in the Conversation and Execution panes. This helps the user easily locate and reference messages (e.g., "message 12" or "global message 25") during debugging and walkthrough reviews.

## Confirmed Facts

1. **Event Model**: The model `DisplayEvent` in `src/CXTracer/Models/DisplayEvent.cs` represents log events. It already contains properties like `Pane`, `LineNumber`, and `Timestamp`.
2. **Views**: Card layouts are defined in the `DataTemplate` of `ConversationListBox` and `ExecutionListBox` inside `src/CXTracer/Views/MainWindow.axaml`.
3. **Data Loading**: Events are populated via `LoadSelectedSessionAsync` and live-updated via `HandleSessionFileChangedOnUiThreadAsync` in `src/CXTracer/ViewModels/MainWindowViewModel.cs`.

## Requirements

1. **Model Properties**: Add `ColumnSequenceText` and `MergedSequenceText` properties to `DisplayEvent`.
2. **Sequential Calculation**:
   - Order all events in the session belonging to `EventPane.Conversation` and `EventPane.Execution` chronologically.
   - Assign each event a single-pane sequence number (index within its column) and a merged sequence number (index across both columns).
3. **UI Layout**:
   - Display **two separate circular capsules** to the left of the card's timestamp inside the header.
   - Sized with `MinWidth="22"` to comfortably host up to 4 digits.
   - Do not display the capsules for Raw events (where the property values are empty).
   - Ensure the horizontal line at half the height of the time text on the right and the capsules are perfectly centered and aligned.
4. **Color Styling**: Use warm-neutral colors (`#F3EBE0` and `#EDE5DA` backgrounds, `#DDD5C8` borders, `#8A8178` foregrounds) to keep it clean and metadata-focused.
5. **Stability**: Sequence numbers must remain stable/fixed when filtering or searching events.

## Acceptance Criteria

- [x] Every Conversation and Execution card displays two separate sequence number capsules next to its timestamp.
- [x] The sequence numbers are sorted chronologically by the event's sort timestamp.
- [x] The capsules and the timestamp text are perfectly aligned vertically.
- [x] Filtering the list via the search TextBox or ComboBox does not change the sequence numbers.
- [x] Live updates append new events with correctly computed incremented sequence numbers.
