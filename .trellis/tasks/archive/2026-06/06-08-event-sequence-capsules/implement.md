# Implementation Plan: Event Sequence Capsules

This checklist outlines the steps required to implement the sequence number capsules in both Conversation and Execution lists.

## Execution Checklist

### Phase 1: Model Updates
- [x] **Modify `DisplayEvent.cs`** in [DisplayEvent.cs](file:///k:/Code/ACTIVE/CXTracer/src/CXTracer/Models/DisplayEvent.cs):
  - Add string properties `ColumnSequenceText` and `MergedSequenceText` via `[ObservableProperty]`.

### Phase 2: ViewModel Logic
- [x] **Add Sequence Number Helper** in [MainWindowViewModel.cs](file:///k:/Code/ACTIVE/CXTracer/src/CXTracer/ViewModels/MainWindowViewModel.cs):
  - Implement `UpdateEventSequenceNumbers()` to sort conversation & execution events chronologically and assign individual sequence numbers.
- [x] **Integrate Helper into Session Loading**:
  - In `LoadSelectedSessionAsync`, call `UpdateEventSequenceNumbers()` right after populating `_allEvents`.
- [x] **Integrate Helper into Live Update**:
  - In `HandleSessionFileChangedOnUiThreadAsync`, call `UpdateEventSequenceNumbers()` after adding new events to `_allEvents` but before processing visible collections.

### Phase 3: UI Design
- [x] **Modify `MainWindow.axaml`** in [MainWindow.axaml](file:///k:/Code/ACTIVE/CXTracer/src/CXTracer/Views/MainWindow.axaml):
  - In `ConversationListBox` and `ExecutionListBox` ItemTemplates, wrap the timestamp `TextBlock` in a horizontal `StackPanel` and insert the dual Capsule Border elements with explicit `VerticalAlignment="Center"` and `MinWidth="22"`.

---

## Verification Plan

### Automated Build Check
- Run `dotnet build` to confirm compiles successfully without errors or warnings.

### Manual Verification
1. **Load Session**: Load a session and verify that each card in Conversation and Execution shows two sequence capsules (e.g. `1` and `1`, `2` and `3` etc.) to the left of the timestamp.
2. **Alignment Check**: Verify that the horizontal center of the circles aligns perfectly with the center of the timestamp text.
3. **Raw Pane Check**: Verify that expander/raw cards do NOT display any capsules.
4. **Filter Stability**:
   - Type a search query.
   - Verify that the sequence numbers on visible cards remain unchanged (absolute).
5. **Live Update Check**:
   - Trigger a live update (add log events to the active session file).
   - Verify that newly appended cards appear with correct, sequentially incremented numbers.
