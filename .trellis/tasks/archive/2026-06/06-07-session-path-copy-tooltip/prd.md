# Session Path Copy & Tooltip Support

## Goal

Add full support for viewing the complete session path on hover (Tooltip) and copying the path to the clipboard on right-click for the active session path label in the top bar of CXTracer.

## User Value

Currently, the active session path is truncated in the top bar with no way to see the full path or copy it. Adding hover tooltips and right-click copy actions allows users to quickly inspect their workspace context and share or use the current session path externally.

## Confirmed Facts

- **Active Session Path Property**: Bound to `SelectedSessionPath` on `MainWindowViewModel` (displayed in `MainWindow.axaml` at line 255).
- **Tooltips Syntax**: Tooltips are defined using the attached property `ToolTip.Tip="..."` (e.g. `ToolTip.Tip="{Binding SelectedSessionPath}"`).
- **Clipboard & Toast Feedback**: `MainWindow.axaml.cs` has an existing pattern for clipboard copy operations via `TopLevel.GetTopLevel(this)?.Clipboard` accompanied by SukiUI toasts (`ToastManager`).

## Requirements

### REQ-1: Session Path Hover Tooltip
- **AC-1.1**: Hovering over the active session path text in the header bar must show a tooltip displaying the full path.
- **AC-1.2**: The tooltip style must match the existing `ToolTip.Tip` tooltips used in navigation buttons.

### REQ-2: Right-Click Copy Support
- **AC-2.1**: Right-clicking the active session path text block must copy the full path to the system clipboard.
- **AC-2.2**: Right-clicking must trigger a SukiUI toast notification reading "Session path copied to clipboard." or similar context-specific confirmation.
