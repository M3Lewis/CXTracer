# Design

## Scope

This is a local Trellis customization. The source of truth remains `.trellis/workflow.md`; platform files and shared skills are updated only where they must agree with that workflow.

## File Changes

- `.trellis/project-profile.md`: new stack profile for C# / .NET / Avalonia validation.
- `.agents/skills/trellis-explore/SKILL.md`: new thinker skill for hidden-rule exploration during failed or ambiguous work.
- `.trellis/workflow.md`: update Phase 2, active routing, and workflow-state breadcrumbs.
- `.agents/skills/trellis-check/SKILL.md`: require stack-aware validation and a Verification Matrix.
- `.agents/skills/trellis-update-spec/SKILL.md`: gate promotion of `implicit-rules.md` findings into specs.
- `.codex/agents/trellis-implement.toml`: require test-plan-aware implementation and project-profile validation.
- `.codex/agents/trellis-check.toml`: require functional evidence review, not just lint/type-check.

## Behavior

Implementation should start by deriving a test plan from `prd.md` acceptance criteria. Check should verify that each acceptance criterion and user-visible behavior change has evidence: an automated test, an exact existing test/command, or a documented manual verification waiver.

When check fails, the user corrects the target, or the same issue repeats, the next step is `trellis-explore` before another implementation attempt. `trellis-explore` writes inferred hidden rules to the active task, then implementation/check can use those findings. Spec promotion still happens only through `trellis-update-spec` and its curator gate.

## Compatibility

No task lifecycle status or hook parser changes are needed. Existing `[workflow-state:*]` blocks stay in place; only their body text changes.
