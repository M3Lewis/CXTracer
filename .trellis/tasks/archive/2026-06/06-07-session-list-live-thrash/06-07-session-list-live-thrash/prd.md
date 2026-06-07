# PRD: Session List Live Update Thrashing

## Goal
Optimize session list updates on active session writes. Avoid clearing and rebuild of the entire `Sessions` list.

## User Value
Reduces UI lag, thrashing, layout recalculations, and visual flickering when new events are actively appended in the background.

## Confirmed Facts
- `MainWindowViewModel.UpsertSession` clears and rebuilds the `Sessions` collection every time a log line is appended.
- Rebuilding the collection destroys and recreates Avalonia controls/layouts for all cards.

## Requirements
- **REQ-1**: Perform incremental/in-place movements of the updated session card to the top (index 0) of the list instead of clearing the collection.
- **REQ-2**: Re-sort or rebuild only when a completely new session is detected or order changes are necessary.
