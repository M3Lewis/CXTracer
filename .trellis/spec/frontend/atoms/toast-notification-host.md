---
id: frontend.toast-notification-host
type: architecture_decision
priority: must
applies_when:
  - displaying transient alerts/notifications to the user
  - copying text to the clipboard and notifying the user
code_anchors:
  - src/CXTracer/Views/MainWindow.axaml
  - src/CXTracer/Views/MainWindow.axaml.cs
  - src/CXTracer/ViewModels/MainWindowViewModel.cs
verify:
  - clipboard copy triggers a toast notification
  - escape key and backdrop click dismiss overlay modals
source:
  kind: human_confirmed
  ref: task-2026-06-07-message-detail-popup
last_checked: 2026-06-07
---

# Rule

When adding transient notifications (Toasts) or Clipboard copy operations, adhere to the following contracts:

1. **SukiUI Toast Host Configuration**:
   - The `<suki:SukiToastHost>` must be placed within `<suki:SukiWindow.Hosts>` inside `MainWindow.axaml`.
   - The toast host's `Manager` property must be bound to a ViewModel property (`ToastManager`) of type `ISukiToastManager` (instantiated as `new SukiToastManager()`).

2. **Asynchronous Clipboard Operations**:
   - Use `IClipboard.SetTextAsync` to write to the clipboard.
   - Do not reference deprecated static clipboard interfaces. Retrieve `IClipboard` from the nearest `Visual` context:
     ```csharp
     var topLevel = TopLevel.GetTopLevel(visualElement);
     if (topLevel?.Clipboard is { } clipboard)
     {
         await clipboard.SetTextAsync(text);
     }
     ```

3. **User Action Acknowledgment & Contextual Messages**:
   - Actions that copy content (like right-clicking cards or session paths) must trigger a SukiUI toast feedback message that explicitly describes the copied entity (e.g. "Event text copied to clipboard." or "Session path copied to clipboard.") to ensure clear, contextual user feedback.

4. **Overlay Event Interception**:
   - When custom overlay dialogs/popups are active, intercept navigation/escape keys at the Window level (`KeyDown`) to prevent focus drift in background lists.

# Why

- Toast hosts must reside in the window hosts container to render correctly over other layout layers without visual truncation.
- Using standard `IClipboard` ensures compatibility with Avalonia 11/12 and avoids runtime access exceptions across platform boundaries.
- Modal focus/key guarding prevents background scroll viewport offset shifts when navigation keys are pressed while reading detail popups.
