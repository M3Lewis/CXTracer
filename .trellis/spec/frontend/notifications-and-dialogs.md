# Notifications & Dialogs

SukiUI provides first-class hosts and manager APIs for dialogs and toasts. Use them through MVVM-friendly managers instead of static shell calls.

## Preferred Host Setup

Declare hosts once in `MainWindow` or the root `SukiWindow`.

```xml
<suki:SukiWindow.Hosts>
    <suki:SukiDialogHost Manager="{Binding DialogManager}" />
    <suki:SukiToastHost Manager="{Binding ToastManager}" />
</suki:SukiWindow.Hosts>
```

## Preferred ViewModel Surface

Expose managers from the shell or desktop presentation layer.

```csharp
public class ShellViewModel
{
    public ISukiDialogManager DialogManager { get; } = new SukiDialogManager();
    public ISukiToastManager ToastManager { get; } = new SukiToastManager();
}
```

## Dialog Usage

Use dialogs for blocking confirmation, destructive actions, and structured user decisions.

```csharp
DialogManager.CreateDialog()
    .WithTitle("Delete file")
    .WithContent("This action cannot be undone.")
    .WithActionButton("Cancel", _ => { }, true)
    .WithActionButton("Delete", _ => Delete(), true, "Flat", "Accent")
    .TryShow();
```

Recommended dialog capabilities:

- explicit title
- clear action buttons
- dismissal policy defined intentionally
- message-box type when severity matters

## Toast Usage

Use toasts for transient feedback, background progress, and short-lived action prompts.

```csharp
ToastManager.CreateToast()
    .WithTitle("Saved")
    .WithContent("Settings were updated successfully.")
    .OfType(NotificationType.Success)
    .Dismiss().After(TimeSpan.FromSeconds(3))
    .Queue();
```

## Selection Rules

- use **dialog** for blocking decisions or multi-step acknowledgment
- use **toast** for transient success, warning, or background progress
- use **InfoBar** when the message must remain visible in the page layout

## Boundary Rules

- hosts belong only in `SukiWindow.Hosts`
- non-UI layers must not instantiate or manage Suki controls directly
- avoid static global manager singletons unless the app shell itself intentionally owns them
- do not call dialog or toast APIs from code-behind for business workflows
