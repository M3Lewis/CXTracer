# Audit src code health

## Goal

Audit the maintainability of the `src/` codebase, then fix the accepted code-health findings that make future changes harder.

## Requirements

- Inspect `src/` before selecting findings.
- Focus on maintainability risks: unnecessary coupling, duplicated responsibilities, unclear module ownership, wrong abstractions, and poor locality.
- Base every finding on concrete evidence from files, functions, imports, call sites, or repeated rules.
- Do not report style-only issues.
- Do not propose broad rewrites.
- Report no more than 3 findings unless the user asks for a broader audit.
- Fix the reported findings with narrow, reviewable changes.
- Preserve the existing Avalonia MVVM boundaries: Views handle Avalonia event details, ViewModels handle state and validation, Services handle transcript IO.

## Acceptance Criteria

- [ ] The audit covers the main `src/` structure before selecting findings.
- [ ] The final report includes a PASS / WATCH / FAIL verdict.
- [ ] Each finding includes evidence, maintenance risk, a small fix, a grill question, and a recommended answer.
- [ ] Findings are written to the task research directory for durable task context.
- [ ] Transcript shared-open file access has one service-level owner used by both session scanning and reading.
- [ ] Shortcut capture behavior is shared between `MainWindow` and `SettingsWindow` without moving Avalonia key/event details into ViewModels.
- [ ] The unused eager session scan API is removed or made explicit so startup metadata-only scanning remains the default path.
- [ ] Focused validation proves the changed service and UI code still compiles.

## Notes

- This started as a lightweight read-only audit task. The user then requested fixing the reported findings in the same task.
