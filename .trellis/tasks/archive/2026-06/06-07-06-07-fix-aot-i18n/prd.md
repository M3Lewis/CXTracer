# Fix Native AOT Startup Crash and Language Switcher

## Goal

Fix the desktop application's startup crash under Native AOT caused by dynamic `ResourceInclude` loading, and resolve the language switching failure where language settings could not be updated dynamically.

## Requirements

1. **Native AOT Compatibility**: Ensure the application can start without reflection-based runtime crash when loading resources.
2. **Dynamic Language Swapping**: Allow instant swapping of UI languages (English and Simplified Chinese) via the Settings window without requiring an application restart.
3. **PDB/Debug Symbol Exclusion**: Ensure the published zip package excludes heavy `.pdb` files.

## Acceptance Criteria

- [x] Application successfully compiles under Native AOT (`dotnet publish -c Release`).
- [x] Application launches successfully (no startup crash on resource loading).
- [x] Dynamic language toggle in Settings updates all UI text instantly.
- [x] Release package size is kept clean and under 25MB (by excluding PDBs).

## Solution & Implementation Details

- **App.axaml Merged Dictionaries**: Statically declared both `en-US.axaml` and `zh-CN.axaml` resource dictionaries inside `App.axaml` so they are compiled at build-time.
- **Identifier Keys**: Added `<sys:String x:Key="LocalizationLanguage">en/zh</sys:String>` in both XAML dictionaries.
- **IResourceDictionary Casting & Caching**: Updated `MainWindowViewModel.ApplyLanguage` to typecast items in `MergedDictionaries` to `IResourceDictionary`, identify the correct ones via the `LocalizationLanguage` key, cache them, and then add/remove them dynamically at runtime.
