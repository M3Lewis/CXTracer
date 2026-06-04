# Native AOT Publish Status

## Status

Native AOT publish now completes on this machine.

## Command

```powershell
dotnet publish src\CodexLens\CodexLens.csproj -c Release -r win-x64 /p:PublishAot=true
```

## Result

The command completes and writes publish output to:

```text
src\CodexLens\bin\Release\net8.0\win-x64\publish\
```

Publish output contains 6 files totaling about 281 MiB. `CodexLens.exe` is about 30 MiB.

## Remaining Warnings

The publish output has zero trim/AOT warnings attributable to `CodexLens` application code.

Two third-party warnings remain from `Avalonia.Controls.DataGrid` 11.3.13:

```text
warning IL2104: Assembly 'Avalonia.Controls.DataGrid' produced trim warnings.
warning IL3053: Assembly 'Avalonia.Controls.DataGrid' produced AOT analysis warnings.
```

The current UI does not use a DataGrid control path. Required smoke coverage focused on the SukiUI/Avalonia controls used by the app: main `SukiWindow`, settings `SukiWindow`, toolbar buttons, checkbox, list content, transcript panes, and scrollable content.

## Work Completed

- Added Release-scoped Native AOT publish settings to `src/CodexLens/CodexLens.csproj`.
- Added source-generated JSON metadata in `src/CodexLens/Services/AppJsonContext.cs`.
- Updated `AppSettingsService` to load/save settings through source-generated JSON metadata.
- Updated `CodexEventParser.PrettyCompact` to serialize `JsonElement` through source-generated JSON metadata.
- Confirmed existing AXAML data templates already have explicit `x:DataType`.

## Verified

```powershell
dotnet restore .\CodexLens.sln
dotnet build .\CodexLens.sln --no-restore
dotnet build .\CodexLens.sln --configuration Release --no-restore
dotnet publish src\CodexLens\CodexLens.csproj -c Release -r win-x64 /p:PublishAot=true
```

Debug and Release builds passed with 0 warnings and 0 errors. Publish completed with only the third-party DataGrid warnings listed above.

Smoke testing used repository sample data from `samples\sample-rollout.jsonl` and UI Automation against the published executable. No private transcript content, private paths, screenshots, or logs are committed.
