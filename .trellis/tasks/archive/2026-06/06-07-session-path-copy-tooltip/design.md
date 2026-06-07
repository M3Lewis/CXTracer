# Design: Session Path Copy & Tooltip Support

## View Updates

In `MainWindow.axaml`, update the `<TextBlock Text="{Binding SelectedSessionPath}" .../>` at line 255:
1. Add `ToolTip.Tip="{Binding SelectedSessionPath}"` so Avalonia automatically displays the full path on mouse hover.
2. Add `PointerPressed="SessionPath_PointerPressed"` to listen for pointer clicks on the TextBlock.

```xml
<TextBlock Text="{Binding SelectedSessionPath}"
           FontFamily="Consolas"
           TextTrimming="CharacterEllipsis"
           Foreground="#3A3732"
           ToolTip.Tip="{Binding SelectedSessionPath}"
           PointerPressed="SessionPath_PointerPressed"/>
```

## Code-Behind Updates

In `MainWindow.axaml.cs`:
1. Implement `SessionPath_PointerPressed`:
   - Check if `DataContext` is `MainWindowViewModel viewModel`.
   - Check if the right pointer button is pressed (`e.GetCurrentPoint(null).Properties.IsRightButtonPressed`).
   - If true, call `CopyToClipboardAsync(viewModel.SelectedSessionPath, "Session path")` and set `e.Handled = true`.

2. Refactor `CopyEventTextAsync` into a generalized clipboard helper:
   ```csharp
   private async Task CopyToClipboardAsync(string text, string entityName)
   {
       if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
       {
           await clipboard.SetTextAsync(text);

           if (DataContext is MainWindowViewModel viewModel)
           {
               viewModel.ToastManager.CreateToast()
                   .WithTitle("Copied")
                   .WithContent($"{entityName} copied to clipboard.")
                   .Dismiss().After(TimeSpan.FromSeconds(2))
                   .Queue();
           }
       }
   }
   ```
