# Verification

## Verification Matrix

| Requirement / Acceptance Criteria | Verification Type | Evidence | Status |
|---|---|---|---|
| `src/CodexLens/CodexLens.csproj` supports Native AOT publish for Release `win-x64` | build/publish | `dotnet publish src\CodexLens\CodexLens.csproj -c Release -r win-x64 /p:PublishAot=true` completed | Pass |
| No Linux or macOS Native AOT publish support is claimed | static review | `CodexLens.csproj` declares only `RuntimeIdentifiers=win-x64` and scopes default `RuntimeIdentifier` to `win-x64`; PRD/final report do not claim other RIDs | Pass |
| AOT publish settings are conditionally scoped so Debug remains JIT/non-AOT | build | `dotnet build .\CodexLens.sln --no-restore` produced `bin\Debug\net8.0\CodexLens.dll`; `dotnet build .\CodexLens.sln --configuration Release --no-restore` produced `bin\Release\net8.0\win-x64\CodexLens.dll`; both passed with 0 warnings/errors | Pass |
| Native AOT publish completes or blocker is documented | publish | Publish completed; no blocker remains | Pass |
| Publish output has zero trim/AOT warnings from `CodexLens` app code | publish log review | Publish emitted only `Avalonia.Controls.DataGrid` IL2104/IL3053 warnings | Pass |
| Third-party trim/AOT warnings are recorded with affected package/control area and smoke coverage | manual review | `blocker.md` records `Avalonia.Controls.DataGrid` warnings; app smoke covers used SukiUI/Avalonia controls and no DataGrid path is currently used | Pass |
| `AppSettingsService` uses source-generated JSON metadata and preserves settings shape/output | service smoke | Temporary ignored console smoke: `IndentedSettings=True`, `ReloadedSync=True`, `ReloadedShortcut=Ctrl+Shift+K` | Pass |
| `CodexEventParser` avoids reflection-based `JsonSerializer.Serialize` for `JsonElement` pretty-printing | static review | `rg "JsonSerializer\.(Serialize|Deserialize)" src/CodexLens` shows all app calls use `AppJsonContext.Default.*` metadata | Pass |
| AXAML `DataTemplate` declarations that bind model data have explicit `x:DataType` | static review | `rg "DataTemplate|x:DataType" src/CodexLens -g "*.axaml"` shows every `DataTemplate` has `x:DataType` | Pass |
| Published AOT app launches and main window renders | UI Automation smoke | Published `CodexLens.exe` stayed running, title `Codex Lens`, non-zero main window handle, closed cleanly | Pass |
| Published AOT app scans sample data and renders Conversation/Execution/Raw panes | UI Automation smoke | Set root path to `samples`, clicked Refresh, observed `sample-rollout`, `Conversation`, `Execution`, and `Raw` markers | Pass |
| Published AOT app opens Settings and SukiUI settings controls render | UI Automation smoke | Clicked `Settings`; observed `Navigation`, `Synchronized navigation`, `Shortcut`, and `Close` markers | Pass |
| Required smoke test uses repository sample data | manual review | Smoke root was changed to repository `samples` directory containing `sample-rollout.jsonl` | Pass |
| Optional real-session smoke test is not committed | git status/review | No private paths, transcript contents, screenshots, or logs staged; only task artifacts and source changes will be committed | Pass |
| Final report includes publish path and approximate size; publish output remains untracked | git status / file measurement | Publish path `src\CodexLens\bin\Release\net8.0\win-x64\publish\`; total about 281 MiB; ignored by `bin/` rule | Pass |
| README and user-facing docs are unchanged | git status | Dirty tracked source files are project/services only; README unchanged | Pass |
| Existing settings files remain compatible without migration | service smoke/static review | `AppSettings` field names unchanged; source-gen context includes `AppSettings` and `ShortcutGesture`; reload smoke passed | Pass |
| No dependency version upgrades, SukiUI replacement, installer, or release packaging are included | diff review | `CodexLens.csproj` package versions unchanged; no packaging/installer files changed | Pass |

## Commands Run

```powershell
Get-Process CodexLens -ErrorAction SilentlyContinue | Select-Object Id,ProcessName,Path
rg -n "DataTemplate|x:DataType" src/CodexLens -g "*.axaml"
rg -n "JsonSerializer\.(Serialize|Deserialize)" src/CodexLens
dotnet build .\CodexLens.sln --no-restore
dotnet restore .\CodexLens.sln
dotnet build .\CodexLens.sln --no-restore
dotnet build .\CodexLens.sln --configuration Release --no-restore
dotnet publish src\CodexLens\CodexLens.csproj -c Release -r win-x64 /p:PublishAot=true
dotnet run --project src\CodexLens\bin\aot-smoke-settings-test\aot-smoke-settings-test.csproj --configuration Debug
```

UI Automation smoke tests launched:

```text
src\CodexLens\bin\Release\net8.0\win-x64\publish\CodexLens.exe
```

## Notes

- Initial `dotnet build .\CodexLens.sln --no-restore` failed because restored assets were missing `Microsoft.NET.ILLink.Tasks` 8.0.19. `dotnet restore .\CodexLens.sln` fixed the restore asset state, and the subsequent no-restore build passed.
- UI smoke artifacts were created under ignored `src\CodexLens\bin\...` paths and are not part of the commit.
- Publish output remains in the default publish directory for inspection and is not staged.
- Spec update decision: no `.trellis/spec/` change was made. The AOT settings and source-generated JSON contracts are expressed directly in code, while the task-specific publish/smoke evidence belongs in this task artifact.
