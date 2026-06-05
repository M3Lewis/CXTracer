# Code Health Audit

## Scope

Read-only audit of `src/`, focused on maintainability risks rather than style.

## Verdict

WATCH

## Findings

### 1. [WARN] Transcript file read rules are duplicated in scanner and reader

Evidence:
- `src/CodexLens/Services/SessionReader.cs:224` defines `OpenReadShared()` with `FileAccess.Read`, `FileShare.ReadWrite | FileShare.Delete`, `64 * 1024`, and `FileOptions.SequentialScan`.
- `src/CodexLens/Services/SessionScanner.cs:94` repeats the same file-open policy in `ReadHeadLines()`.
- `src/CodexLens/Services/SessionScanner.cs:119` repeats the same policy again in `EstimateLineCount()`.
- `.trellis/spec/backend/quality-guidelines.md:7` through `.trellis/spec/backend/quality-guidelines.md:10` make this read-only/shared-open behavior a project safety contract.

Problem:
- This is not just ordinary duplication. The repeated code encodes the app's safety contract for active Codex transcript files. A future change to buffer size, share flags, or file options must be remembered in three places, and a drifted copy can reintroduce file-lock or active-session read bugs.

Small Fix:
- Add a narrowly named service helper, for example `SessionFileAccess.OpenReadShared(string filePath)`, under `Services/`.
- Use it from `SessionReader` and `SessionScanner`.
- Do not create a generic `FileUtils`; this helper should own only the transcript-file read contract.

Grill Question:
- Is shared-open transcript reading a reusable project rule or an implementation detail of each service?

Recommended Answer:
- It is a reusable project rule. Give it one owner inside `Services/` and keep scanner/reader focused on scan and tail semantics.

### 2. [WARN] Shortcut capture behavior is implemented twice in view code-behind

Evidence:
- `src/CodexLens/Views/MainWindow.axaml.cs:76` through `src/CodexLens/Views/MainWindow.axaml.cs:94` handles capture mode, modifier-only keydown, key normalization, accepted capture, rejection message, and `e.Handled`.
- `src/CodexLens/Views/SettingsWindow.axaml.cs:26` through `src/CodexLens/Views/SettingsWindow.axaml.cs:55` implements the same capture path for the settings window.
- `src/CodexLens/Views/ShortcutKeyInput.cs:5` centralizes physical `Avalonia.Input.Key` normalization, but the capture workflow around that helper is still duplicated.
- `.trellis/spec/frontend/atoms/shortcut-capture-contract.md:25` through `.trellis/spec/frontend/atoms/shortcut-capture-contract.md:39` says modifier-only keydown and punctuation shortcut handling are durable rules.

Problem:
- The project already had shortcut regressions, and the durable rule is now split across two event handlers. Any change to capture acceptance, rejection text, or handled-event timing has to be made in both windows. That is exactly the shape that lets one window keep the fix while the other drifts.

Small Fix:
- Keep `ShortcutKeyInput` view-only, but add one shared view helper that handles the capture branch for a supplied target and `KeyEventArgs`.
- Alternatively, expose one small ViewModel-facing capture method that takes normalized modifier/key data and returns whether the key event was consumed.
- Do not move Avalonia `Key` or visual event details into the ViewModel.

Grill Question:
- Should shortcut capture be two window behaviors that happen to match, or one behavior hosted by two windows?

Recommended Answer:
- It should be one behavior hosted by two windows. Physical key normalization remains in `Views`, while capture state and validation stay in the ViewModel/model layer.

### 3. [INFO] `SessionScanner.Scan()` is an unused eager-enrichment entry point

Evidence:
- `src/CodexLens/Services/SessionScanner.cs:19` defines `ScanLight()` and returns metadata-only session entries.
- `src/CodexLens/Services/SessionScanner.cs:35` defines public `Scan()` and calls `Enrich()` for every discovered session.
- `src/CodexLens/ViewModels/MainWindowViewModel.cs:181` uses `ScanLight()` for refresh.
- `src/CodexLens/ViewModels/MainWindowViewModel.cs:362` enriches only the selected session through `TryGetSessionSummary()`.
- A source search found no current call site for `Scan()`.
- `.trellis/spec/backend/quality-guidelines.md:25` says startup session discovery must stay metadata-only and must not read every transcript just to populate the list.

Problem:
- The current runtime path is correct, but the public unused `Scan()` method offers the slower behavior as an equally valid API. A future caller can accidentally choose it and violate the metadata-only discovery rule without touching the scanner internals.

Small Fix:
- Remove `Scan()` if no tests or external callers need it.
- If it is intentionally kept for a future explicit workflow, rename it to something like `ScanWithSummaries()` and document that it reads transcript heads and counts lines.
- Do not make `RefreshAsync()` use eager enrichment.

Grill Question:
- Is eager session enrichment part of the product path, or just leftover capability?

Recommended Answer:
- Treat it as leftover capability until a concrete workflow needs it. The app's main path should keep the light scan plus selected-session enrichment split.

## First Fix

Start with finding 1. The transcript file-open contract is both safety-critical and easy to centralize with a small PR.
