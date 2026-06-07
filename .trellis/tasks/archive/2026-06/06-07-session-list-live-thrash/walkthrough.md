# Walkthrough: Session List Live Update Thrashing Fix

## Changes Made

### ViewModel Layer

#### [MODIFY] [MainWindowViewModel.cs](file:///k:/Code/ACTIVE/CXTracer/src/CXTracer/ViewModels/MainWindowViewModel.cs)
- Modified `UpsertSession(string path)` to remove the inefficient full-list rebuild (`Sessions.Clear()` followed by full `Add` loop).
- Implemented an in-place positioning logic:
  - If the file is modified, its `LastWriteTime` is updated.
  - The method now scans for the target sorted position based on the new `LastWriteTime`.
  - Only if the item's position needs to change (e.g., from index 5 to index 0), does it invoke `Sessions.RemoveAt` and `Sessions.Insert`.
  - Preserved the `SelectedSession` assignment across the move to ensure visual selection remains intact during the transition.
- This entirely eliminates the UI layout destruction and thrashing on the Avalonia `ListBox` side when background updates append log lines.

## Verification & Tests

### Automated Build Verification
Ran:
```bash
dotnet build src/CXTracer/CXTracer.csproj --nologo -v q
```
Output: Built successfully with 0 errors and 0 warnings.
