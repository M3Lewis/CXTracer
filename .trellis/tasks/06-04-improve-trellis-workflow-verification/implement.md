# Implementation Plan

## Checklist

- [x] Add `.trellis/project-profile.md`.
- [x] Add `.agents/skills/trellis-explore/SKILL.md`.
- [x] Update `.trellis/workflow.md` Phase 2 and routing text.
- [x] Update `trellis-check` and `trellis-update-spec` shared skill guidance.
- [x] Update Codex implement/check agent TOML descriptions.
- [x] Start this task with `task.py start`.
- [x] Verify the active task points to this task.
- [x] Search changed files for stale lint/type-check-only completion language.
- [x] Export portable workflow override package for other Trellis projects.

## Test Plan

| Acceptance Criteria | Test Level | Test File / Command | Notes |
|---|---|---|---|
| Project profile exists | static review | `Test-Path .trellis/project-profile.md` | Confirms stack profile was added. |
| `trellis-explore` exists | static review | `Test-Path .agents/skills/trellis-explore/SKILL.md` | Confirms skill was added in shared skill layer. |
| Workflow mentions evidence and explore routing | static review | `rg "Verification Matrix|trellis-explore|project-profile" .trellis/workflow.md` | Confirms breadcrumbs and phase docs were updated. |
| Check skill requires evidence | static review | `rg "Verification Matrix|project-profile|necessary but never sufficient" .agents/skills/trellis-check/SKILL.md` | Confirms check semantics. |
| Update-spec gates implicit rules | static review | `rg "implicit-rules|Curator Gate" .agents/skills/trellis-update-spec/SKILL.md` | Confirms spec promotion semantics. |
| Codex agents aligned | static review | `rg "Verification Matrix|project-profile|test plan" .codex/agents` | Confirms platform entry text. |
