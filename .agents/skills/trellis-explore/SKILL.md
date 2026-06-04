---
name: trellis-explore
description: "Mid-task hidden-rule exploration for Trellis work. Use after check failures, user target corrections, repeated debugging, or unclear component/behavior mapping before another implementation attempt. Writes findings to the active task research directory and does not edit code."
---

# Trellis Explore

Use this skill when implementation or checking is stuck because the project rule, target, or failure cause is unclear. This is a thinker skill: infer hidden rules and write guidance for the next implementation attempt. Do not edit code.

## Triggers

Run `trellis-explore` before another implementation attempt when any of these happens:

- `trellis-check` fails and the cause is not obvious.
- The user says the target or behavior was misunderstood.
- The same bug or failure class appears a second time.
- The intended component, selector, file, API, or behavior is ambiguous.
- A diff touches nearby similar targets and needs target revalidation.

## Inputs To Read

1. Resolve the active task:
   ```bash
   python ./.trellis/scripts/task.py current --source
   ```
2. Read the active task artifacts:
   - `prd.md`
   - `design.md` if present
   - `implement.md` if present
   - existing `research/*.md`
3. Read current evidence:
   - user correction text or screenshots, if present
   - failing command output or test names
   - `git diff`
   - relevant `.trellis/spec/` files
   - relevant source files

## Output File

Write findings to:

```text
.trellis/tasks/<task>/research/implicit-rules.md
```

Create the `research/` directory if needed. If the file already exists, append a new dated entry instead of replacing previous findings.

Use this structure:

```md
# Implicit Rules Exploration

## Entry: YYYY-MM-DD - <short trigger>

### Trigger
Why exploration was required: check failure, user correction, repeated debugging, or unclear target.

### Observed Evidence
Facts only: exact error, diff summary, user wording, failing test, source/spec paths.

### Inferred Hidden Rules
Rules inferred from the evidence. Mark uncertainty clearly.

### Action Guidance
What the next implementation/check attempt should do, and what similar targets are out of scope.

### Confidence
High / Medium / Low, with a one-line reason.
```

## Boundaries

- Write only under the active task's `research/` directory.
- Do not edit code, `.trellis/workflow.md`, `.trellis/spec/`, platform config, or other task directories.
- Do not promote findings directly into `.trellis/spec/`. At task finish, `trellis-update-spec` decides whether any finding is durable, verified, non-redundant, and worth atomizing into specs.
- If evidence is insufficient, write the uncertainty and the exact question or source that would resolve it.

## Handoff

After writing `implicit-rules.md`, return a short summary:

- File written
- Hidden rule candidates
- Next implementation guidance
- Confidence / unresolved uncertainty
