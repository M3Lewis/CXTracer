# Bootstrap Task: Fill Project Development Guidelines

**You (the AI) are running this task. The developer does not read this file.**

The developer just ran `trellis init` on this project for the first time.
`.trellis/` now exists with empty spec scaffolding, and this bootstrap task
exists under `.trellis/tasks/`.

**Your job**: help populate `.trellis/spec/` with this repository's real coding
conventions so future AI sessions follow the current project shape instead of
generic templates.

---

## Status

- [x] Fill backend guidelines
- [x] Fill frontend guidelines
- [x] Add code examples

---

## What Was Documented

The repository is a Windows-first .NET 8 Avalonia desktop app named Codex Lens.
It reads Codex CLI session JSONL files without modifying them.

Backend/service specs now document:

- the current single-project `src/CodexLens` layout
- `Services/` ownership of scanning, reading, watching, and parsing
- no database, no indexes, and no writes to Codex session files
- manual service composition in `App.axaml.cs`
- filesystem/parser error handling and cancellation patterns
- quality checks for read-only IO, parser tolerance, and UI-thread collection updates

Frontend specs now document:

- `SukiTheme` setup in `App.axaml`
- `SukiWindow` setup in `MainWindow.axaml`
- CommunityToolkit.Mvvm source-generated state and commands
- compiled binding and `x:DataType` conventions
- the current dense two-pane transcript UI
- the permitted code-behind exception for view-only scroll navigation
- inline footer status instead of dialogs/toasts

---

## Source Evidence

Existing convention docs:

- `AGENTS.md`

Code examples inspected:

- `src/CodexLens/CodexLens.csproj`
- `src/CodexLens/App.axaml`
- `src/CodexLens/App.axaml.cs`
- `src/CodexLens/Program.cs`
- `src/CodexLens/Views/MainWindow.axaml`
- `src/CodexLens/Views/MainWindow.axaml.cs`
- `src/CodexLens/ViewModels/MainWindowViewModel.cs`
- `src/CodexLens/Services/SessionScanner.cs`
- `src/CodexLens/Services/SessionReader.cs`
- `src/CodexLens/Services/SessionWatcher.cs`
- `src/CodexLens/Services/CodexEventParser.cs`
- `src/CodexLens/Models/DisplayEvent.cs`
- `src/CodexLens/Models/SessionInfo.cs`

---

## Completion

After verification, finish and archive with:

```bash
python ./.trellis/scripts/task.py finish
python ./.trellis/scripts/task.py archive 00-bootstrap-guidelines
```
