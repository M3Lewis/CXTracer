# Improve Trellis workflow verification and exploration

## Goal

Apply the local Trellis workflow changes described in `workflow-change.md` so future tasks verify user-facing behavior with evidence, infer hidden rules when check/debug loops stall, and choose validation commands from the project stack instead of assuming TypeScript / Node.

## Requirements

- Add a project profile that records the repository stack, validation commands, and testing strategy for C# / .NET / Avalonia work.
- Add a local `trellis-explore` skill for mid-task hidden-rule inference. It must persist findings to the active task under `research/implicit-rules.md` and must not edit code directly.
- Update `.trellis/workflow.md` Phase 2 and in-progress breadcrumbs so implementation includes a PRD-derived test plan, check requires a Verification Matrix, and unclear or repeated failures route through `trellis-explore`.
- Update local check/update-spec guidance so build/lint/type-check passing is necessary but never sufficient, and only durable evidence-backed explore findings are promoted to specs.
- Keep Codex platform agent descriptions aligned with the workflow changes.

## Acceptance Criteria

- [x] `.trellis/project-profile.md` exists and describes C# / .NET / Avalonia validation and testing layers.
- [x] `.agents/skills/trellis-explore/SKILL.md` exists and defines triggers, inputs, output format, and write boundaries.
- [x] `.trellis/workflow.md` Phase 2.1, 2.2, 2.3, active routing, and in-progress workflow-state blocks mention project-specific validation, Verification Matrix evidence, and optional `trellis-explore` before repeated implementation attempts.
- [x] `.agents/skills/trellis-check/SKILL.md` requires stack-aware checks and a Verification Matrix mapping acceptance criteria to evidence.
- [x] `.agents/skills/trellis-update-spec/SKILL.md` explicitly treats `implicit-rules.md` as candidate input that must pass the curator gate before spec promotion.
- [x] `.codex/agents/trellis-implement.toml` and `.codex/agents/trellis-check.toml` match the updated implementation/check responsibilities.

## Notes

- Source requirement: `workflow-change.md`.
- Out of scope: modifying Trellis upstream, changing `.trellis/scripts/`, changing the Native AOT task files, or committing unrelated WIP.
