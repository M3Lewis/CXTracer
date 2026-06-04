# Implementation Plan

## Pre-Implementation

- Read `aot_assessment.md`.
- Read frontend specs before AXAML edits:
  - `.trellis/spec/frontend/index.md`
  - relevant atoms for binding/style changes if any AXAML is touched
- Read backend/service specs if available before service changes.
- Check current git status and keep untracked `aot_assessment.md` out of all commits; the user explicitly chose not to commit it.
- Check for a running `CodexLens` process before builds.
- Try build/publish commands in the existing project layout first; request network or elevation only after a concrete restore, runtime pack, toolchain, or permission blocker is observed.
- Do not install native toolchains; report missing Visual Studio Build Tools or Windows SDK blockers for user action.

## Steps

1. Update project publish configuration
   - Edit `src/CodexLens/CodexLens.csproj`.
   - Add conditionally scoped Native AOT publish settings so Release publish defaults to AOT without breaking Debug/JIT development.

2. Add source-generated JSON context
   - Create `src/CodexLens/Services/AppJsonContext.cs`.
   - Include metadata for `AppSettings` and `JsonElement`.
   - Configure JSON options to preserve current indented settings output and pretty raw JSON formatting.

3. Replace reflection-based JSON calls
   - Update `AppSettingsService.Load` to deserialize via generated `AppSettings` metadata.
   - Update `AppSettingsService.Save` to serialize via generated `AppSettings` metadata.
   - Preserve the existing settings path, JSON field shape, field semantics, and indented output.
   - Update `CodexEventParser.PrettyCompact` to serialize `JsonElement` via generated metadata/options.
   - Preserve existing fallback behavior when JSON parsing fails.

4. Verify AXAML typed binding coverage
   - Confirm every `DataTemplate` with bindings has `x:DataType`.
   - Add missing `x:DataType` only where needed.
   - Do not rewrite ordinary view-level bindings just for style.

5. Build and publish
   - Run `dotnet build .\CodexLens.sln --no-restore`.
   - Run `dotnet publish src\CodexLens\CodexLens.csproj -c Release -r win-x64 /p:PublishAot=true`.
   - Fix every trim/AOT warning attributable to `CodexLens` application code.
   - Record any third-party trim/AOT warning that remains, including the package/control area and smoke-test coverage.
   - Leave publish output in the default publish directory for user inspection; do not commit or delete it.

6. Runtime smoke test
   - Launch the published AOT executable.
   - Verify main window renders.
   - Scan a repository sample directory or copied non-sensitive transcript directory.
   - Select a session and verify Conversation, Execution, and Raw panes render.
   - Open Settings, change a setting, close/reopen, and verify persistence.
   - Verify an existing settings file still loads without migration.
   - Verify SukiUI controls used by the app render without runtime exceptions.
   - Optionally verify a real local Codex sessions directory without committing private paths, transcript contents, screenshots, or logs.
   - If SukiUI blocks publish or runtime, stop after documenting exact failure output and smallest failing control path; do not replace SukiUI.
   - If a dependency version appears to block AOT, stop and request approval before upgrading .NET, Avalonia, SukiUI, or CommunityToolkit packages.

## Validation Commands

```powershell
Get-Process CodexLens -ErrorAction SilentlyContinue | Select-Object Id,ProcessName,Path
dotnet build .\CodexLens.sln --no-restore
dotnet publish src\CodexLens\CodexLens.csproj -c Release -r win-x64 /p:PublishAot=true
```

If the first command shows a running app that would lock the original build output, stop and tell the user to close it before continuing.

## Review Gates

- Confirm the diff does not include unrelated UI changes or generated build outputs.
- Confirm `aot_assessment.md` remains untracked and is not included in any commit.
- Confirm the work stops at Native AOT publish plus local smoke test; do not add packaging or installer changes.
- Confirm no SukiUI removal/replacement changes are present.
- Confirm no package version upgrades are present unless separately approved.
- Confirm validation is scoped to `win-x64`; do not claim Linux/macOS AOT support.
- Confirm source-generated JSON path covers every `JsonSerializer` call in application code.
- Confirm publish output and smoke-test result are reported with exact command names.
- Confirm application-code trim/AOT warnings are zero, or the task is not complete.
- Confirm required smoke tests use sample or non-sensitive copied data; real-session checks are optional and private.
- Confirm any escalation/network request is justified by exact command output from the original build/publish path.
- Confirm missing native toolchain blockers, if any, are reported without installing system components.
- Confirm publish output path and approximate size are reported, and publish output is not staged.
- Confirm README and user-facing docs are unchanged.
