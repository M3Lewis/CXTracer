# Implementation Plan

## Proposed Edits

### Views & ViewModels
- Modify `MainWindowViewModel.cs` to add state management and commands for full-size viewing.
- Modify `MainWindow.axaml` to build the floating overlay panel.
- Update `zh-CN.axaml` and `en-US.axaml` with localization strings.

### Models & Services
- Modify `DisplayEvent.cs` to search payload JSON recursively for base64 data and map placeholders.
- Add upward-scanning file search fallback in `ResolvePath`.
- Create `ImagePathToBitmapConverter.cs` for direct loading.

## Verification
- Test file: `rollout-2026-05-31T20-50-52-019e7e16-4fd4-7553-8644-4754cbe1621a.jsonl`.
- Click image -> viewer opens -> toggle scale modes -> close.
