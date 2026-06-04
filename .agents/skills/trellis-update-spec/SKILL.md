---
name: trellis-update-spec
description: "Curates and captures durable, verifiable, non-code-redundant contracts and conventions into .trellis/spec/. Use when a task, bug fix, review, or discussion may have produced reusable project knowledge."
---

# Update Code-Spec - Curate Then Capture Executable Contracts

When you learn something valuable from debugging, implementation, review, or discussion, use this skill to decide whether it deserves to enter `.trellis/spec/`, then update the relevant code-spec documents only for accepted knowledge.

**Timing**: After completing a task, fixing a bug, or discovering a new pattern

---

## Curator Gate (CRITICAL)

Do not write more documentation by default. Trellis specs are operational constraints for coding agents, not casual notes.

Before editing `.trellis/spec/`, test every candidate against these questions:

1. Is this knowledge already obvious from current source code?
2. Is it stable enough to preserve?
3. Is it atomic, or does it contain multiple rules?
4. Does it have a clear trigger condition?
5. Does it explain why the rule exists?
6. Is it verifiable by test, static check, review checklist, or human confirmation?
7. Does it have code anchors or a clear scope?
8. Could it become stale if code changes?
9. Does another spec already say the same thing?
10. Would this help an agent make a better code change?

If the answer to #10 is no, do not keep it. If the agent can discover the fact by reading current source code, do not encode it as a Trellis spec.

### What Specs Should Preserve

Keep only knowledge that code does not reliably express:

- business invariants
- compatibility constraints
- historical pitfalls
- operational assumptions
- cross-system contracts
- security or compliance constraints
- verification requirements
- team decisions that are not obvious from code
- bug lessons confirmed by humans, tests, incidents, or review

### Source Requirement

Do not promote a candidate into an active spec unless it is supported by at least one of:

- explicit user confirmation
- bug or incident history
- failing test or regression test
- PR review comment
- existing product requirement
- external API/client contract
- observed repeated agent failure confirmed by the user

Unsupported candidates are `NEEDS_HUMAN_CONFIRMATION`, not active specs.

### Classification Labels

Classify each candidate as one of:

- `KEEP`: high-signal, still accurate, non-redundant, agent-useful.
- `ATOMIZE`: useful but too broad or narrative. Split into smaller atoms.
- `REWRITE`: useful but vague, unverifiable, missing `why`, triggers, or anchors.
- `MERGE`: duplicates another spec. Keep the stronger version and merge unique knowledge only.
- `DELETE`: restates current code, is obsolete, or provides no durable value.
- `ARCHIVE`: historically interesting but no longer active guidance.
- `NEEDS_HUMAN_CONFIRMATION`: possibly valuable, but not supported by code, tests, user confirmation, incident history, or review evidence.

Only `KEEP`, accepted `ATOMIZE`, accepted `REWRITE`, and accepted `MERGE` results should lead to edits.

### What To Reject

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

Prefer deleting 10 weak specs over adding 1 vague spec.

---

## Code-Spec First Rule (CRITICAL)

In this project, "spec" for implementation work means **code-spec**:
- Executable contracts (not principle-only text)
- Concrete signatures, payload fields, env keys, and boundary behavior
- Testable validation/error behavior

This depth requirement applies only after the curator gate accepts the candidate as durable, non-redundant project knowledge.

If the change touches infra or cross-layer contracts, code-spec depth is mandatory.

### Mandatory Triggers

Apply code-spec depth when the change includes any of:
- New/changed command or API signature
- Cross-layer request/response contract change
- Database schema/migration change
- Infra integration (storage, queue, cache, secrets, env wiring)

### Mandatory Output (7 Sections)

For triggered tasks, include all sections below:
1. Scope / Trigger
2. Signatures (command/API/DB)
3. Contracts (request/response/env)
4. Validation & Error Matrix
5. Good/Base/Bad Cases
6. Tests Required (with assertion points)
7. Wrong vs Correct (at least one pair)

---

## When to Consider Code-Spec Updates

| Trigger | Example | Target Spec |
|---------|---------|-------------|
| **Implemented a feature** | Added a new integration or module | Relevant spec file |
| **Made a design decision** | Chose extensibility pattern over simplicity | Relevant spec + "Design Decisions" section |
| **Fixed a bug** | Found a subtle issue with error handling | Relevant spec (e.g., error-handling docs) |
| **Discovered a pattern** | Found a better way to structure code | Relevant spec file |
| **Hit a gotcha** | Learned that X must be done before Y | Relevant spec + "Common Mistakes" section |
| **Established a convention** | Team agreed on naming pattern | Quality guidelines |
| **New thinking trigger** | "Don't forget to check X before doing Y" | `guides/*.md` (as a checklist item) |

**Key Insight**: Code-spec updates are not just for problems, but not every feature deserves a spec. Capture only decisions and contracts that future AI/developers need to execute safely and cannot reliably infer from code.

---

## Spec Structure Overview

```
.trellis/spec/
├── <layer>/           # Per-layer coding standards (e.g., backend/, frontend/, api/)
│   ├── index.md       # Overview and links
│   └── *.md           # Topic-specific guidelines
└── guides/            # Thinking checklists (NOT coding specs!)
    ├── index.md       # Guide index
    └── *.md           # Topic-specific guides
```

### CRITICAL: Code-Spec vs Guide - Know the Difference

| Type | Location | Purpose | Content Style |
|------|----------|---------|---------------|
| **Code-Spec** | `<layer>/*.md` | Tell AI "how to implement safely" | Signatures, contracts, matrices, cases, test points |
| **Guide** | `guides/*.md` | Help AI "what to think about" | Checklists, questions, pointers to specs |

**Decision Rule**: Ask yourself:

- "This is **how to write** the code" → Put in a spec layer directory
- "This is **what to consider** before writing" → Put in `guides/`

**Example**:

| Learning | Wrong Location | Correct Location |
|----------|----------------|------------------|
| "Use API X not API Y for this task" | ❌ `guides/` (too specific for a thinking guide) | ✅ Relevant spec file (concrete convention) |
| "Remember to check X when doing Y" | ❌ Spec file (too abstract for a spec) | ✅ `guides/` (thinking checklist) |

**Guides should be short checklists that point to specs**, not duplicate the detailed rules.

---

## Update Process

### Step 1: Identify What You Learned

Answer these questions:

1. **What did you learn?** (Be specific)
2. **Why is it important?** (What problem does it prevent?)
3. **What evidence supports it?** (User confirmation, test, bug, review, external contract, or repeated confirmed failure)
4. **Where does it belong?** (Which spec file?)

### Step 2: Run the Curator Gate

Classify the candidate with the labels above.

- If it is `DELETE`, `ARCHIVE`, or `NEEDS_HUMAN_CONFIRMATION`, do not write an active spec.
- If it restates current code, skip it.
- If it duplicates existing spec content, merge only unique knowledge.
- If it is broad or narrative, atomize it before writing.

### Step 3: Classify the Accepted Update Type

| Type | Description | Action |
|------|-------------|--------|
| **Design Decision** | Why we chose approach X over Y | Add to "Design Decisions" section |
| **Project Convention** | How we do X in this project | Add to relevant section with examples |
| **New Pattern** | A reusable approach discovered | Add to "Patterns" section |
| **Forbidden Pattern** | Something that causes problems | Add to "Anti-patterns" or "Don't" section |
| **Common Mistake** | Easy-to-make error | Add to "Common Mistakes" section |
| **Convention** | Agreed-upon standard | Add to relevant section |
| **Gotcha** | Non-obvious behavior | Add warning callout |

### Step 4: Read the Target Code-Spec

Before editing, read the current code-spec to:
- Understand existing structure
- Avoid duplicating content
- Find the right section for your update

```bash
cat .trellis/spec/<category>/<file>.md
```

### Step 5: Make the Update

Follow these principles:

1. **Be Specific**: Include concrete examples, not just abstract rules
2. **Explain Why**: State the problem this prevents
3. **Show Contracts**: Add signatures, payload fields, and error behavior
4. **Show Code**: Add code snippets for key patterns
5. **Keep it Short**: One concept per section

### Step 6: Update the Index (if needed)

If you added a new section or the code-spec status changed, update the category's `index.md`.

---

## Update Templates

### Preferred Spec Atom Shape

Prefer Markdown files or sections that are atomic and reviewable. When creating a new active spec atom, include:

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

Use this shape when adding a standalone spec file:

```markdown
---
id: order.cancel.worker-idempotency
type: pitfall
priority: must
applies_when:
  - modifying order cancellation worker
  - changing retry behavior
code_anchors:
  - src/Orders/OrderCancellationWorker.cs
verify:
  - duplicate worker execution is covered by a test
source:
  kind: human_confirmed
  ref: task-2026-06-02-order-cancel-worker
last_checked: 2026-06-02
---

# Rule

Order cancellation worker retries must not execute refund or audit side effects more than once.

# Why

A previous retry bug caused duplicate refund attempts when the worker retried after a timeout.
```

Recommended `type` values: `invariant`, `compatibility`, `pitfall`, `security`, `performance`, `operational`, `architecture_decision`, `testing_requirement`, `external_contract`, `migration_note`.

Use `priority: must` only when violating the rule can break production, compatibility, security, data integrity, money movement, compliance, or major user behavior.

### Mandatory Template for Infra/Cross-Layer Work

```markdown
## Scenario: <name>

### 1. Scope / Trigger
- Trigger: <why this requires code-spec depth>

### 2. Signatures
- Backend command/API/DB signature(s)

### 3. Contracts
- Request fields (name, type, constraints)
- Response fields (name, type, constraints)
- Environment keys (required/optional)

### 4. Validation & Error Matrix
- <condition> -> <error>

### 5. Good/Base/Bad Cases
- Good: ...
- Base: ...
- Bad: ...

### 6. Tests Required
- Unit/Integration/E2E with assertion points

### 7. Wrong vs Correct
#### Wrong
...
#### Correct
...
```

### Adding a Design Decision

```markdown
### Design Decision: [Decision Name]

**Context**: What problem were we solving?

**Options Considered**:
1. Option A - brief description
2. Option B - brief description

**Decision**: We chose Option X because...

**Example**:
\`\`\`typescript
// How it's implemented
code example
\`\`\`

**Extensibility**: How to extend this in the future...
```

### Adding a Project Convention

```markdown
### Convention: [Convention Name]

**What**: Brief description of the convention.

**Why**: Why we do it this way in this project.

**Example**:
\`\`\`typescript
// How to follow this convention
code example
\`\`\`

**Related**: Links to related conventions or specs.
```

### Adding a New Pattern

```markdown
### Pattern Name

**Problem**: What problem does this solve?

**Solution**: Brief description of the approach.

**Example**:
\`\`\`
// Good
code example

// Bad
code example
\`\`\`

**Why**: Explanation of why this works better.
```

### Adding a Forbidden Pattern

```markdown
### Don't: Pattern Name

**Problem**:
\`\`\`
// Don't do this
bad code example
\`\`\`

**Why it's bad**: Explanation of the issue.

**Instead**:
\`\`\`
// Do this instead
good code example
\`\`\`
```

### Adding a Common Mistake

```markdown
### Common Mistake: Description

**Symptom**: What goes wrong

**Cause**: Why this happens

**Fix**: How to correct it

**Prevention**: How to avoid it in the future
```

### Adding a Gotcha

```markdown
> **Warning**: Brief description of the non-obvious behavior.
>
> Details about when this happens and how to handle it.
```

---

## Interactive Mode

If you're unsure what to update, answer these prompts:

1. **What did you just finish?**
   - [ ] Fixed a bug
   - [ ] Implemented a feature
   - [ ] Refactored code
   - [ ] Had a discussion about approach

2. **What did you learn or decide?**
   - Design decision (why X over Y)
   - Project convention (how we do X)
   - Non-obvious behavior (gotcha)
   - Better approach (pattern)

3. **Does it pass the curator gate?**
   - It is not obvious from source code
   - It is stable, atomic, scoped, and verifiable
   - It has evidence
   - It would help a future agent make a better code change

4. **Which area does it relate to?**
   - [ ] Backend code
   - [ ] Frontend code
   - [ ] Cross-layer data flow
   - [ ] Code organization/reuse
   - [ ] Quality/testing

---

## Quality Checklist

Before finishing your code-spec update:

- [ ] Did the candidate pass the curator gate?
- [ ] Is it non-redundant with current code and existing specs?
- [ ] Is there evidence for promoting it to an active spec?
- [ ] Does it have a trigger condition?
- [ ] Does it have scope or code anchors?
- [ ] Is it verifiable by test, static check, review, or human confirmation?
- [ ] Is the content specific and actionable?
- [ ] Did you include a code example?
- [ ] Did you explain WHY, not just WHAT?
- [ ] Did you include executable signatures/contracts?
- [ ] Did you include validation and error matrix?
- [ ] Did you include Good/Base/Bad cases?
- [ ] Did you include required tests with assertion points?
- [ ] Is it in the right code-spec file?
- [ ] Does it duplicate existing content?
- [ ] Would a new team member understand it?

---

## Relationship to Other Commands

```
Development Flow:
  Learn something → `update-spec` (Trellis command) → Knowledge captured
       ↑                                  ↓
  `break-loop` (Trellis command) ←──────────────────── Future sessions benefit
  (deep bug analysis)
```

- ``break-loop` (Trellis command)` - Analyzes bugs deeply, often reveals spec updates needed
- ``update-spec` (Trellis command)` - Actually makes the updates
- ``spec-curator` (Trellis skill)` - Use separately for broad review, pruning, atomization, or stale-spec cleanup across `.trellis/spec/`
- ``finish-work` (Trellis command)` - Reminds you to check if specs need updates

---

## Core Philosophy

> **Code-specs are living documents, but smaller is better when every remaining rule is high-signal.**

The goal is **institutional memory**:
- What one person learns, everyone benefits from
- What AI learns in one session, persists to future sessions
- Mistakes become documented guardrails

The boundary is strict: specs capture durable implementation constraints and verified lessons, not source summaries or speculative advice.
