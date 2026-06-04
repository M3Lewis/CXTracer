# Convert CodexLens to Native AOT

## Goal

Convert the Avalonia CodexLens desktop project to a Native AOT publishable application using aot_assessment.md and current code as source material.

## Requirements

- Convert `src/CodexLens` into a Native AOT publishable Avalonia desktop application for Windows x64.
- Cross-platform AOT is out of scope; only Windows x64 is required.
- Success means Native AOT publish succeeds and the published executable passes local smoke testing.
- Release packaging is out of scope: no installer, versioned distribution bundle, release notes, or packaging workflow.
- Use `aot_assessment.md` and the current source as the planning baseline.
- Preserve current desktop functionality:
  - session scanning and loading from the configured Codex sessions root
  - session search/filtering and selected-session pinning
  - Conversation, Execution, and Raw event panes
  - settings window, synchronized navigation setting, and shortcut capture
  - live session file watcher updates
- Keep Debug/JIT development workflow available; Native AOT requirements should primarily affect Release/publish.
- Configure the project so Release publish defaults to Native AOT; Debug/JIT build workflow must remain unaffected.
- Eliminate known AOT/trim risks from reflection-based JSON serialization by using source-generated `System.Text.Json` metadata.
- Preserve existing settings file path, `AppSettings` JSON shape, field semantics, and indented JSON output.
- Treat trim/AOT warnings from application code as failures that must be fixed.
- Third-party trim/AOT warnings may remain only if they are documented and covered by relevant smoke tests.
- Verify AXAML bindings and typed templates remain compatible with compiled binding/AOT behavior.
- Validate SukiUI/Avalonia runtime behavior with an actual Native AOT publish on `win-x64`.
- Required smoke testing must use repository sample or copied non-sensitive transcript data.
- Real local Codex sessions may be used for optional manual confirmation only; do not commit real paths, transcript contents, screenshots, or logs.
- Do not introduce broad UI redesign, new MVVM framework, or unrelated parser/service refactors.
- Do not replace or remove SukiUI in this task; if SukiUI blocks Native AOT publish or runtime, document the blocker and stop.
- Do not upgrade .NET, Avalonia, SukiUI, or CommunityToolkit package versions unless the current version is a proven AOT blocker and the user separately approves the upgrade.
- Do not create an installer, distributable package specification, or release workflow.
- Do not update README or user-facing documentation in this task; report AOT commands in Trellis artifacts and final summary only.
- Do not create alternate temporary build directories to bypass a running application process; if build output is locked by a running `CodexLens` process, stop and ask the user to close it.
- If Native AOT publish requires restore, runtime packs, toolchain components, network access, or elevated filesystem permissions, first try the original command locally, then request approval with the exact blocker and command.
- Do not install native toolchains, Visual Studio Build Tools, or Windows SDK components in this task; report missing toolchain blockers for the user to handle.
- Keep Native AOT publish output in the default publish directory for inspection; do not commit it and do not delete it automatically.

## Acceptance Criteria

- [ ] `src/CodexLens/CodexLens.csproj` supports Native AOT publish for Release `win-x64`.
- [ ] No Linux or macOS Native AOT publish support is claimed by this task.
- [ ] AOT publish settings are conditionally scoped so Debug builds remain JIT/non-AOT.
- [ ] `dotnet build .\CodexLens.sln --no-restore` succeeds after the code changes.
- [ ] `dotnet publish src\CodexLens\CodexLens.csproj -c Release -r win-x64 /p:PublishAot=true` completes or any remaining blocker is documented with exact warning/error output and a rollback path.
- [ ] Publish output has zero trim/AOT warnings attributable to `CodexLens` application code.
- [ ] Any third-party trim/AOT warnings are recorded with affected package/control area and matching smoke-test coverage.
- [ ] `AppSettingsService` uses source-generated JSON metadata for `AppSettings` load/save.
- [ ] `CodexEventParser` avoids reflection-based `JsonSerializer.Serialize` for pretty-printing `JsonElement`.
- [ ] All AXAML `DataTemplate` declarations that bind model data have explicit `x:DataType`.
- [ ] Published AOT app launches and the main window renders.
- [ ] Published AOT app can scan a sessions directory, select a session, show Conversation/Execution/Raw panes, and open/close Settings.
- [ ] Required smoke test uses repository sample data or copied non-sensitive test data.
- [ ] Optional real-session smoke test, if performed, is reported without committing private paths or transcript contents.
- [ ] Any network/elevation request during publish is tied to a concrete restore, runtime pack, toolchain, or permission blocker observed from the original command.
- [ ] Missing native toolchain blockers are reported with exact error output and required component guidance, without attempting installation.
- [ ] Final report includes the publish output path and approximate size; publish output remains untracked.
- [ ] README and user-facing docs are unchanged.
- [ ] Settings persist and reload through the source-generated JSON path.
- [ ] Existing settings files remain compatible without migration.
- [ ] SukiUI controls used by the app are smoke-tested in the AOT binary.
- [ ] If SukiUI blocks AOT, the task records the exact publish/runtime failure and smallest failing control path instead of replacing SukiUI.
- [ ] No dependency version upgrades are included unless separately approved after a concrete AOT blocker is observed.

## Notes

- Source assessment: `aot_assessment.md`.
- `aot_assessment.md` is local reference material and must not be committed.
- This is a complex task; `design.md` and `implement.md` are required before starting implementation.
