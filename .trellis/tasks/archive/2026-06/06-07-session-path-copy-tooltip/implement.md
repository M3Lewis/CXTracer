# Implementation Checklist

## Step 1: Update MainWindow.axaml

- [ ] Locate the `<TextBlock Text="{Binding SelectedSessionPath}" .../>` in the top bar.
- [ ] Add `ToolTip.Tip="{Binding SelectedSessionPath}"` property.
- [ ] Add `PointerPressed="SessionPath_PointerPressed"` handler.

**File**: `src/CXTracer/Views/MainWindow.axaml`

## Step 2: Update MainWindow.axaml.cs

- [ ] Rename `CopyEventTextAsync` to `CopyToClipboardAsync` and generalize its signature to support custom `entityName`.
- [ ] Update callers of `CopyEventTextAsync` to use `CopyToClipboardAsync(evt.Text, "Event text")`.
- [ ] Implement `SessionPath_PointerPressed` to call `CopyToClipboardAsync(viewModel.SelectedSessionPath, "Session path")` when right-clicked.

**File**: `src/CXTracer/Views/MainWindow.axaml.cs`

## Validation

- Build the project:
  ```bash
  dotnet build src/CXTracer/CXTracer.csproj --nologo -v q
  ```
- Run the application.
- Hover the mouse cursor over the session path at the top.
- Verify that a tooltip appears displaying the full, untruncated file path.
- Right-click on the session path.
- Verify that a toast notification pops up reading "Session path copied to clipboard."
- Paste into a text editor to verify the clipboard contains the correct, full file path.
