---
name: trellis-spec-curator
description: "Use when creating, reviewing, pruning, or improving Trellis specs. Converts narrative, duplicate, stale, or code-redundant docs into atomic, verifiable, code-anchored spec atoms."
---

# Trellis Spec Curator

## Purpose

Improve the quality of `.trellis/spec/`.

Do not write more documentation by default. Keep specs small, accurate, atomic, verifiable, and useful to coding agents.

Code is already readable by the agent. Do not duplicate facts that can be reliably discovered from current source code.

Preserve only knowledge that code does not reliably express:

- business invariants
- compatibility constraints
- historical pitfalls
- operational assumptions
- cross-system contracts
- security or compliance constraints
- verification requirements
- team decisions that are not obvious from code
- bug lessons confirmed by humans, tests, incidents, or review

## Core Rule

If the agent can discover the fact by reading current source code, do not encode it as a Trellis spec.

Good specs explain what the code cannot explain. Bad specs restate the code.

## When To Use

Use this skill when:

- the user asks to improve Trellis specs
- a new spec is being added
- a task completed and produced a reusable lesson
- a bug fix revealed a pitfall worth preserving
- specs feel bloated, stale, duplicated, or narrative
- an agent was misled by existing documentation
- a spec references files or behavior that may have changed
- the user asks whether a piece of knowledge should become a spec

Do not use this skill for normal implementation unless the task includes spec maintenance.

## Inputs To Inspect

Inspect only what is needed:

1. `.trellis/spec/`
2. `.trellis/tasks/`
3. `.trellis/workspace/` or recent task journals, if present
4. `AGENTS.md`
5. related source files only when needed to check redundancy or staleness
6. recent bug reports, review comments, failing tests, or user-provided lessons

Do not scan the entire repository by default. Use targeted search.

## Spec Quality Test

For every candidate spec, answer:

1. Is this knowledge already obvious from current code?
2. Is this knowledge stable enough to preserve?
3. Is it atomic, or does it contain multiple rules?
4. Does it have a clear trigger condition?
5. Does it explain why the rule exists?
6. Is it verifiable by test, static check, review checklist, or human confirmation?
7. Does it have code anchors or scope?
8. Could it become stale if code changes?
9. Does another spec already say the same thing?
10. Would this help an agent make a better code change?

If the answer to #10 is no, do not keep it.

## Classification Labels

Use one label for each reviewed spec, paragraph, or rule:

- `KEEP`: high-signal, still accurate, non-redundant, agent-useful.
- `ATOMIZE`: useful but too broad or narrative. Split into smaller atoms.
- `REWRITE`: useful but vague, unverifiable, missing `why`, missing triggers, or missing anchors.
- `MERGE`: duplicates another spec. Keep the stronger version and merge only unique knowledge.
- `DELETE`: restates current code, is obsolete, or provides no durable value.
- `ARCHIVE`: historically interesting but no longer active guidance.
- `NEEDS_HUMAN_CONFIRMATION`: possibly valuable, but not supported by code, tests, user confirmation, incident history, or review evidence.

## What To Reject

Reject specs that merely say:

- which method calls which service
- which class inherits which base class
- which endpoint exists
- which files contain which logic
- which DTO fields currently exist
- what the current implementation does
- generic advice like "be careful"
- generic architecture commentary
- long historical narratives with no actionable rule
- rules without trigger conditions
- rules without a reason
- rules that cannot be verified or reviewed

## Spec Atom Format

Active spec atoms must be Markdown files with YAML frontmatter.

Store active atoms under the owning spec layer's `atoms/` directory:

```text
.trellis/spec/<layer>/atoms/<atom-name>.md
.trellis/spec/<package>/<layer>/atoms/<atom-name>.md
```

Keep normal layer documents such as `component-guidelines.md`, `state-management.md`, and `quality-guidelines.md` as overview/routing documents. They may link to atoms, but they should not duplicate atom rules.

The layer `index.md` must be the entry point:

- list normal guideline documents
- list or group active atoms by trigger/domain
- tell agents to read applicable atoms based on `applies_when`
- keep links current when atoms are moved, merged, archived, or deleted

Do not place active atoms beside normal guideline files unless the layer has not adopted an `atoms/` directory yet. If an `atoms/` directory exists, same-level atom files are `REWRITE` and should be moved.

Each active spec atom should have:

- `id`
- `type`
- `priority`
- `applies_when`
- `code_anchors` or `scope`
- `verify` or `review`
- `source`
- `last_checked`
- `Rule`
- `Why`

Use this shape:

```md
---
id: order.cancel.worker-idempotency
type: pitfall
priority: must
applies_when:
  - modifying order cancellation worker
  - modifying refund side effects
  - changing retry behavior
code_anchors:
  - src/Orders/OrderCancellationWorker.cs
  - src/Orders/OrderService.cs
verify:
  - duplicate worker execution is covered by a test
  - refund uses an idempotency key or durable guard
source:
  kind: human_confirmed
  ref: task-2026-06-02-order-cancel-worker
last_checked: 2026-06-02
---

# Rule

Order cancellation worker retries must not execute refund or audit side effects more than once.

# Why

A previous retry bug caused duplicate refund attempts when the worker retried after a timeout.

# Do

Use a stable cancellation operation id or idempotency key.

# Do Not

Do not rely only on in-memory flags or worker attempt count.
```

If a required field is missing, classify as `REWRITE` unless the spec should be deleted.

## Recommended Types

Use one of:

- `invariant`
- `compatibility`
- `pitfall`
- `security`
- `performance`
- `operational`
- `architecture_decision`
- `testing_requirement`
- `external_contract`
- `migration_note`

Avoid vague types like `note`, `misc`, `general`, or `best_practice`.

## Priority Rules

- `must`: violating this can break production, compatibility, security, data integrity, money movement, compliance, or major user behavior.
- `should`: strong team convention or recurring maintainability issue.
- `may`: useful guidance, but not required.

Do not mark something `must` without a concrete reason.

## Source Rules

Do not promote a spec atom unless it is supported by at least one of:

- explicit user confirmation
- bug or incident history
- failing test or regression test
- PR review comment
- existing product requirement
- external API/client contract
- observed repeated agent failure
- documented team decision

If unsupported, keep it as `NEEDS_HUMAN_CONFIRMATION`.

## Staleness Check

A spec may be stale if:

- referenced files no longer exist
- referenced symbols no longer exist
- code anchors changed meaning
- tests contradict the spec
- another spec supersedes it
- `last_checked` is old and the anchored code changed recently
- it describes implementation details instead of durable constraints

When stale, do not silently update the rule. Propose one of:

- update anchors
- rewrite the rule
- archive the spec
- delete the spec
- ask for human confirmation

## Workflow

1. Inventory relevant spec files.
2. Classify each meaningful paragraph or rule.
3. Compare questionable specs against targeted source files only.
4. Rewrite useful knowledge into candidate atoms.
5. Generate a reviewable patch proposal grouped by new, rewritten, merged, archived, deleted, and human-confirmation items.
6. Apply changes only when the user has asked for implementation or explicitly accepts the patch plan.

## Output Format

Use this report shape:

```md
# Spec Curator Report

## Summary

Reviewed: N files
Keep: N
Rewrite: N
Atomize: N
Merge: N
Delete: N
Archive: N
Needs human confirmation: N

## High-Confidence Changes

### 1. Rewrite `...`

Reason:

Proposed atom:

## Deletions Proposed

### 1. Delete paragraph from `...`

Reason:

## Human Confirmation Needed

### 1. Candidate: `...`

Question for human:

Evidence needed:

## Patch Plan

1. Create ...
2. Rewrite ...
3. Archive ...
4. Delete ...
```

## Anti-Patterns

Do not:

- write broad architecture essays
- summarize source files
- preserve every lesson forever
- convert every task into a spec
- mark guesses as rules
- promote unconfirmed agent conclusions
- create specs without `applies_when`
- create specs without `why`
- create specs without verification or review path
- mix active atom files with ordinary guideline files when an `atoms/` directory exists
- duplicate atom rules inside overview documents instead of linking from them
- use specs as a replacement for reading code
- silently change business rules

## Promotion Rule

A candidate lesson becomes an active spec only when one of these is true:

1. The user explicitly confirms it.
2. A test or bug proves it.
3. A PR review states it as a rule.
4. Existing product or external contract requires it.
5. The same agent failure happened repeatedly and the user confirms the pattern.

Otherwise, keep it as a candidate, not an active spec.

## Minimalism Rule

Prefer deleting 10 weak specs over adding 1 vague spec.

A smaller spec library is better if every remaining rule is high-signal.

## Final Reminder

Trellis specs are not casual human documentation. They are operational constraints for AI agents.

Keep them atomic, durable, scoped, verifiable, non-redundant, and human-confirmed.
