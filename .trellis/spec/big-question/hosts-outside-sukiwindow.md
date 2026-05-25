# Hosts Declared Outside SukiWindow

## Symptom

Dialogs or toasts do not show, or a page declares host markup that has no effect.

## Root Cause

`SukiWindow.Hosts` only works on `SukiWindow`. SukiUI's host documentation explicitly says it is not valid on normal page views.

## Wrong Pattern

```xml
<UserControl>
    <suki:SukiWindow.Hosts>
        <suki:SukiToastHost />
    </suki:SukiWindow.Hosts>
</UserControl>
```

## Correct Pattern

Declare hosts once in the root shell window:

```xml
<suki:SukiWindow>
    <suki:SukiWindow.Hosts>
        <suki:SukiDialogHost Manager="{Binding DialogManager}" />
        <suki:SukiToastHost Manager="{Binding ToastManager}" />
    </suki:SukiWindow.Hosts>
</suki:SukiWindow>
```

## Prevention

- treat hosts as shell infrastructure, not page content
- bind managers from the shell or desktop presentation layer
- do not duplicate hosts across pages
