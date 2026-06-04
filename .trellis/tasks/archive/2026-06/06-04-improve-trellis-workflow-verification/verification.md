# Verification

## Verification Matrix

| Requirement / Acceptance Criteria | Verification Type | Evidence | Status |
|---|---|---|---|
| `.trellis/project-profile.md` exists and describes C# / .NET / Avalonia validation and testing layers | static review | `rg "C#|Avalonia|dotnet build|dotnet test|necessary but never sufficient" .trellis/project-profile.md` | Pass |
| `.agents/skills/trellis-explore/SKILL.md` exists and defines triggers, inputs, output format, and write boundaries | static review | `rg "Triggers|Inputs To Read|implicit-rules.md|Boundaries" .agents/skills/trellis-explore/SKILL.md` | Pass |
| `.trellis/workflow.md` Phase 2.1, 2.2, 2.3, routing, and in-progress blocks mention project-specific validation, Verification Matrix evidence, and optional `trellis-explore` | static review | `rg "Verification Matrix|trellis-explore|project-profile|implicit-rules|test plan" .trellis/workflow.md` | Pass |
| `trellis-check` requires stack-aware checks and a Verification Matrix | static review | `rg "Verification Matrix|project-profile|necessary but never sufficient" .agents/skills/trellis-check/SKILL.md` | Pass |
| `trellis-update-spec` gates `implicit-rules.md` promotion through the curator gate | static review | `rg "implicit-rules.md|curator gate|NEEDS_HUMAN_CONFIRMATION" .agents/skills/trellis-update-spec/SKILL.md` | Pass |
| Codex implement/check agents match updated responsibilities | static review | `rg "Verification Matrix|project-profile|test plan|necessary but never sufficient" .codex/agents` | Pass |
| Portable override package contains current workflow files and coverage instructions | static review + hash comparison | `Get-ChildItem -Recurse .\trellis-workflow-portable-files`; SHA256 pair comparison for 10 copied files; `rg "Target path|覆盖|Verification Matrix|trellis-explore|project-profile|Minimal Set" .\trellis-workflow-portable-files` | Pass |

## Commands Run

- `python ./.trellis/scripts/task.py current --source`
- `python ./.trellis/scripts/task.py validate .trellis/tasks/06-04-improve-trellis-workflow-verification`
- `rg -n "Verification Matrix|trellis-explore|project-profile|implicit-rules|necessary but never sufficient|test plan" .trellis/workflow.md .agents/skills .codex/agents .trellis/project-profile.md`
- `rg -n "finish by running project lint|ensure lint and type-check|Run lint and typecheck|lint/type-check pass|typecheck to verify|Stuck / fixed same bug multiple times" .trellis/workflow.md .agents/skills .codex/agents`
- `git -c safe.directory=K:/Code/ACTIVE/CodexLens diff --check -- .trellis .agents .codex`
- `Get-ChildItem -Recurse .\trellis-workflow-portable-files`
- SHA256 pair comparison between source files and `trellis-workflow-portable-files/files/**`
- `rg -n "Target path|覆盖|Verification Matrix|trellis-explore|project-profile|Minimal Set" .\trellis-workflow-portable-files`

## Notes

- `git diff --check` returned no whitespace errors. It printed line-ending normalization warnings for existing tracked files because Git will convert LF to CRLF on checkout/touch in this working tree.
- Full application build/test was not run because this change is Trellis documentation/configuration, not C# application code.
