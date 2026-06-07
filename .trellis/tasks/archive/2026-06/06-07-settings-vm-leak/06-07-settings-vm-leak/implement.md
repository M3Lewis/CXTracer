# Implementation Checklist - Settings VM Memory Leak Fix

- [ ] Modify `MainWindow.Settings_Click` in `MainWindow.axaml.cs` to capture `SettingsWindowViewModel` and call `Dispose()` when the settings window closes.
- [ ] Build the project to verify compilation:
  ```bash
  dotnet build src/CXTracer/CXTracer.csproj --nologo -v q
  ```
