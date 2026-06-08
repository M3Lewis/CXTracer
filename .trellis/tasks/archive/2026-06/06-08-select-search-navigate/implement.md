# Implementation Plan: Select Search Item and Resume Navigation

This checklist outlines the step-by-step changes required to implement card selection, search navigation, and global navigation resumption.

## Execution Checklist

### Phase 1: View Model Selection Preservation
- [ ] **Modify `ApplyFilterAsync`** in [MainWindowViewModel.cs](file:///k:/Code/ACTIVE/CXTracer/src/CXTracer/ViewModels/MainWindowViewModel.cs):
  - Store `var previousSelected = CurrentTranscriptEvent;` at the very beginning of the method.
  - Remove the unconditional `SetCurrentTranscriptEvent(null);` statement at line 767.
  - At the end of the method (after the loop and `UpdateVisibleEventCount()`), check if `previousSelected` is still visible:
    - If `previousSelected.Pane == EventPane.Conversation && ConversationEvents.Contains(previousSelected)`, set it back.
    - Else if `previousSelected.Pane == EventPane.Execution && ExecutionEvents.Contains(previousSelected)`, set it back.
    - Otherwise, set `SetCurrentTranscriptEvent(null)`.

### Phase 2: View Click Selection & Focus Shift
- [ ] **Modify `CardBorder_PointerPressed`** in [MainWindow.axaml.cs](file:///k:/Code/ACTIVE/CXTracer/src/CXTracer/Views/MainWindow.axaml.cs):
  - On single left-click:
    - Call `viewModel.SetCurrentTranscriptEvent(evt)`.
    - Unfocus the TextBox by setting focus to the window: `this.Focus();`.
  - On double left-click (`e.ClickCount == 2`):
    - Call `viewModel.ShowDetailPopup(evt)`.
- [ ] **Modify `MainWindow.axaml`**:
  - Set `<Setter Property="Focusable" Value="False" />` on the `ListBox.transcriptList ListBoxItem` style to prevent items from capturing keyboard focus and intercepting arrow keys.

### Phase 3: Keyboard Bypass for Arrow Keys
- [ ] **Modify `Window_KeyDown`** in [MainWindow.axaml.cs](file:///k:/Code/ACTIVE/CXTracer/src/CXTracer/Views/MainWindow.axaml.cs):
  - Change the text input focus check (around line 168):
    - If `IsTextInputFocused(e.Source)` is true, check if `e.Key` is `Key.Up` or `Key.Down`.
    - If it is not one of those keys, return early. Otherwise, let it proceed to handle list navigation.

### Phase 4: Scroll into View on Filter Completion
- [ ] **Add Custom Event in ViewModel** in [MainWindowViewModel.cs](file:///k:/Code/ACTIVE/CXTracer/src/CXTracer/ViewModels/MainWindowViewModel.cs):
  - Add `public event Action<DisplayEvent>? FilterAppliedScrollRequest;`.
  - In `ApplyFilterAsync`, after restoring selection, invoke `FilterAppliedScrollRequest?.Invoke(previousSelected);` if it remains visible.
- [ ] **Subscribe to Event in View** in [MainWindow.axaml.cs](file:///k:/Code/ACTIVE/CXTracer/src/CXTracer/Views/MainWindow.axaml.cs):
  - Override `OnDataContextChanged` to subscribe to the `FilterAppliedScrollRequest` event on the ViewModel.
  - In the handler, call `ScrollEventIntoView(selectedEvent)` inside `Dispatcher.UIThread.Post` with `DispatcherPriority.Background` to guarantee post-layout alignment.

---

## Verification Plan

### Automated Build Check
- Run `dotnet build` to ensure no compiler warnings or errors are introduced.

### Manual Verification Matrix
1. **Single-click Selection**: Click a card in either pane. Verify it gets the active border highlight, and the detail popup does NOT open.
2. **Double-click Detail**: Double-click a card. Verify the detail popup opens.
3. **Typing and Arrow Navigation**:
   - Type a keyword in the search box (e.g. "tool").
   - Click a card in the search results (input box loses focus, card is highlighted).
   - Press Up/Down arrow keys. Verify selection moves strictly among the filtered search results.
   - Now click inside the search TextBox (input box gains focus).
   - Press Up/Down arrow keys. Verify selection still moves among the filtered search results.
4. **Clear and Resume**:
   - Select a card in the search results.
   - Click the "x" (Clear Search) button.
   - Verify the search box clears, the full list is restored, the selected card remains highlighted, and it is automatically scrolled to the top of the pane.
   - Press Up/Down arrow keys. Verify you can seamlessly navigate to the global events preceding or succeeding that card.
