# Notifications and Dialogs

CXTracer currently uses inline status instead of modal dialogs or toast notifications.

## Current Feedback Pattern

`MainWindowViewModel.StatusMessage` is displayed in the footer. `IsBusy` drives the footer `ProgressBar`.

Current examples:

- scan start: `Scanning sessions...`
- missing root: status explains that the directory was not found
- read/load failures: status includes the operation failure
- live updates: status reports appended event count or another updated session

This fits the current app because operations are low-risk and non-destructive.

## When to Add Dialogs

Use a dialog only for blocking decisions or explicit confirmation, for example:

- changing a setting that affects many files
- exporting sensitive transcript data
- confirming a future destructive operation

If dialogs are added, declare `SukiDialogHost` under `SukiWindow.Hosts` and expose the manager from the shell ViewModel.

## When to Add Toasts

Use toasts for transient background events that should not replace the footer status, for example:

- background session root became unavailable
- export completed
- live session switched because pinning is off

## Avoid

- Showing modal dialogs for routine parse failures.
- Calling dialog/toast APIs from service classes.
- Adding global static notification managers.
- Logging or displaying full raw transcript content in error messages unless the raw pane is explicitly opened.
