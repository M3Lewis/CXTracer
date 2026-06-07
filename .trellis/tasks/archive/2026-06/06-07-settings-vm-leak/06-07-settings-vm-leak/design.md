# Design: Settings VM Memory Leak Fix

## Proposed Changes

### Views

#### [MODIFY] [MainWindow.axaml.cs](file:///k:/Code/ACTIVE/CXTracer/src/CXTracer/Views/MainWindow.axaml.cs)
- In `Settings_Click`, store the `SettingsWindowViewModel` reference.
- Subscribe to the `Closed` event of `SettingsWindow`.
- In the `Closed` event handler, call `Dispose()` on the settings view model.

```csharp
    private void Settings_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            if (_settingsWindow is { IsVisible: true })
            {
                _settingsWindow.Activate();
                return;
            }

            var settingsVm = new SettingsWindowViewModel(viewModel);
            _settingsWindow = new SettingsWindow
            {
                DataContext = settingsVm
            };
            _settingsWindow.Closed += (_, _) =>
            {
                _settingsWindow = null;
                settingsVm.Dispose();
            };
            _settingsWindow.Show(this);
        }
    }
```
