# Walkthrough: Settings VM Memory Leak Fix

## Changes Made

### Frontend Code-Behind

#### [MODIFY] [MainWindow.axaml.cs](file:///k:/Code/ACTIVE/CXTracer/src/CXTracer/Views/MainWindow.axaml.cs)
- Modified `Settings_Click` to explicitly capture the newly instantiated `SettingsWindowViewModel`.
- In the `Closed` event handler of the settings window, called `Dispose()` on the captured view model.
- This ensures that the event subscription `_main.PropertyChanged += OnMainPropertyChanged` (established inside the view model's constructor) is correctly un-registered (`-=`) when the window closes, fully resolving the memory leak.

## Verification & Tests

### Automated Build Verification
Ran:
```bash
dotnet build src/CXTracer/CXTracer.csproj --nologo -v q
```
Output: Built successfully with 0 errors and 0 warnings.
