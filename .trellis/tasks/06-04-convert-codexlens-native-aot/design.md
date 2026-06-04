# Native AOT Conversion Design

## Scope

The implementation target is the single Avalonia desktop project under `src/CodexLens`. The work should make the app publishable as a Native AOT Windows x64 binary while preserving the existing JIT Debug workflow.

`aot_assessment.md` is local reference material only. It must remain untracked and must not be committed with this task.

The delivery boundary is Native AOT publish success plus local smoke testing of the published executable. Installer creation, release packaging, and versioned distribution workflow are separate future work.

Out of scope:

- UI redesign unrelated to AOT compatibility.
- Replacing or removing SukiUI, even if Native AOT publish or smoke testing proves it is the blocker.
- Upgrading .NET, Avalonia, SukiUI, or CommunityToolkit package versions without separate approval after a concrete AOT blocker is observed.
- Rewriting the Codex event parser beyond JSON serialization changes required for AOT.
- Creating alternate build output/intermediate directories to bypass a running app process.
- Committing `aot_assessment.md`.
- Creating installers, release bundles, release notes, or packaging automation.
- Supporting Linux or macOS Native AOT publish.
- Updating README or user-facing documentation.

## Project Configuration

Add Native AOT publish settings to `src/CodexLens/CodexLens.csproj` conservatively. Release publish should default to Native AOT; Debug/JIT builds should remain non-AOT.

- `PublishAot` conditionally scoped to Release publish.
- `TrimMode` appropriate for Native AOT.
- Application-code trim/AOT warnings are failures and must be fixed.
- Third-party trim/AOT warnings can remain only when documented and covered by smoke tests for the affected package/control area.

Keep the existing Debug-only `Avalonia.Diagnostics` package condition. Debug builds should continue to run normally.

## JSON Serialization

`AppSettingsService` currently uses reflection-based `JsonSerializer.Deserialize<T>` and `JsonSerializer.Serialize<T>`. Replace this with a source-generated `JsonSerializerContext`, likely under `Services/AppJsonContext.cs`.

This is a serialization mechanism change only. Preserve the existing settings path, `AppSettings` JSON shape, field semantics, and indented output formatting. Existing settings files should continue to load without migration.

Required metadata:

- `AppSettings`
- `JsonElement` for pretty-printing raw JSON

`CodexEventParser` can continue to use `JsonDocument.Parse` and `JsonElement` traversal. Only `JsonSerializer.Serialize(doc.RootElement, ...)` needs to move to generated metadata/options.

## AXAML Bindings

The project already has `AvaloniaUseCompiledBindingsByDefault=true`. Preserve and verify typed binding coverage:

- `MainWindow.axaml` has `x:DataType="vm:MainWindowViewModel"`.
- `SettingsWindow.axaml` has `x:DataType="vm:SettingsWindowViewModel"`.
- Existing `DataTemplate` declarations for `SessionInfo` and `DisplayEvent` already have explicit `x:DataType`.

If implementation introduces new templates, they must include `x:DataType`.

## SukiUI/Avalonia Compatibility

Treat SukiUI as the highest runtime uncertainty. The publish step is not enough; the AOT binary must be launched and smoke-tested against the controls this app actually uses:

- `SukiWindow` main window.
- `SukiTheme` startup/theme resources.
- toolbar buttons/toggles.
- settings `SukiWindow`.
- ListBox, ScrollViewer, Expander, GridSplitter, ProgressBar.

If SukiUI breaks under AOT, document the exact failure and the smallest failing control path, then stop the task as blocked. Do not replace SukiUI in this task.

If SukiUI or another third-party dependency emits trim/AOT warnings but the published app works, record the warning text, package/control area, and smoke-test result.

Do not upgrade dependencies as part of the default AOT conversion. If a current dependency version is a proven AOT blocker, stop and ask for approval before changing package versions.

## Validation Strategy

Validation escalates in layers:

1. Normal compile: `dotnet build .\CodexLens.sln --no-restore`.
2. Native AOT publish: `dotnet publish src\CodexLens\CodexLens.csproj -c Release -r win-x64 /p:PublishAot=true`.
3. Manual smoke test of the published executable.

Required smoke tests should use repository sample data or copied non-sensitive transcript data. A real local Codex sessions directory may be used only as optional manual confirmation; do not commit private paths, transcript contents, screenshots, or logs.

Before any build, check whether `CodexLens.exe` is running. If it is running and would lock build outputs, stop and ask the user to close it.

Do not create extra temporary build folders or override output/intermediate paths merely to bypass an application-process lock.

If publish fails because required runtime packs, restore inputs, toolchain components, network access, or existing build directories are unavailable, first capture the exact failure from the original command. Escalation or network approval is allowed only after that concrete blocker is known.

Installing C++ native toolchains, Visual Studio Build Tools, or Windows SDK components is out of scope. If missing toolchain components block Native AOT publish, report the exact error and required component guidance, then stop.

Native AOT publish output should remain in the default `bin/Release/.../publish` location for user inspection. It is not committed and should not be deleted automatically.

AOT usage commands should be reported in task artifacts and final summary only. README updates are out of scope until the AOT workflow is stable.

## Rollback

Rollback should be straightforward:

- Remove AOT publish properties from `CodexLens.csproj`.
- Revert source-generated JSON context and restore direct `JsonSerializer` calls.
- Keep unrelated UI and parser behavior unchanged so rollback does not affect user-visible workflows.
- Do not include a SukiUI removal path in rollback because SukiUI replacement is out of scope.
- Do not include package upgrade rollback unless a package upgrade is separately approved.
