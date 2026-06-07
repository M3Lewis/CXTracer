# Design: Session List Live Update Thrashing

## Overview

Currently, `UpsertSession` in `MainWindowViewModel` clears the entire `Sessions` collection and re-adds all elements on every file write, destroying Avalonia's virtualized list containers and causing UI thrashing. The fix is to incrementally move the updated session to its new sorted position (usually index 0) without clearing the collection.

## Proposed Changes

### [MODIFY] [MainWindowViewModel.cs](file:///k:/Code/ACTIVE/CXTracer/src/CXTracer/ViewModels/MainWindowViewModel.cs)
- Locate the `UpsertSession(string path)` method.
- If the session doesn't exist, insert it at index 0.
- If the session already exists, update its `LastWriteTime` and `Length`.
- Determine its new sorted position (`targetIndex`) based on `LastWriteTime`.
- If its `targetIndex` is different from its current index (`oldIndex`), remove it from `oldIndex` and insert it at `targetIndex`.
- Avoid calling `Sessions.Clear()` entirely.

```csharp
    private void UpsertSession(string path)
    {
        var info = _scanner.TryGetSession(path);
        if (info is null)
        {
            return;
        }

        var existing = Sessions.FirstOrDefault(s => PathsEqual(s.FilePath, info.FilePath));
        if (existing is null)
        {
            Sessions.Insert(0, info);
            _ = EnrichSingleSessionAsync(info);
        }
        else
        {
            existing.LastWriteTime = info.LastWriteTime;
            existing.Length = info.Length;

            int oldIndex = Sessions.IndexOf(existing);
            int targetIndex = 0;
            while (targetIndex < Sessions.Count && Sessions[targetIndex].LastWriteTime > existing.LastWriteTime)
            {
                targetIndex++;
            }

            if (targetIndex > oldIndex) targetIndex--;

            if (oldIndex != targetIndex)
            {
                _selectionChanging = true;
                var currentSelection = SelectedSession;
                Sessions.RemoveAt(oldIndex);
                Sessions.Insert(targetIndex, existing);
                SelectedSession = currentSelection;
                _selectionChanging = false;
            }
        }

        OnPropertyChanged(nameof(SessionCountText));
    }
```
