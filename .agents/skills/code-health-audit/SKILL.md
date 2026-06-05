---
name: code-health-audit
description: "Periodically audit existing code for maintainability risks: coupling, duplicated responsibilities, unclear module boundaries, wrong abstractions, and poor locality. Use this before the codebase quietly rots."
---

# Code Health Audit

You are auditing code health.

Find the places where the code is becoming harder to change.

Focus on:

- unnecessary coupling
- duplicated responsibilities
- unclear module ownership
- wrong abstractions
- poor locality

Do not look for style issues.
Do not suggest broad rewrites.
Do not create abstractions just to remove duplication.

Read the code first.
Base every finding on concrete evidence from files, functions, imports, call sites, or repeated rules.

Prefer boring, local fixes.

## Audit Questions

Ask these questions while reading the code:

- Does one change require touching too many places?
- Does one module know details that should belong to another module?
- Is the same business rule implemented in more than one place?
- Is this abstraction reducing complexity, or hiding it?
- Do I need to jump across too many files to understand one behavior?
- Is this code shared because it is truly common, or because someone disliked duplication?

## Output

Return only the most important findings.

Use this format:

```md
# Code Health Audit

## Verdict

PASS / WATCH / FAIL

## Findings

### 1. [ERR/WARN/INFO] Title

Evidence:
- Concrete files, functions, imports, or call sites.

Problem:
- What maintenance risk this creates.

Small Fix:
- The smallest useful change.
- What not to do.

Grill Question:
- The design question this code is avoiding.

Recommended Answer:
- Your recommended answer.
```

## Severity

Use `ERR` when the issue already makes changes spread across multiple places.

Use `WARN` when the code is trending toward unclear ownership, coupling, or wrong abstraction.

Use `INFO` only when the improvement is useful but not worth a dedicated refactor.

## Rules

Be blunt.

A little duplication is better than a bad abstraction.

A helper with no clear owner is not a design.

A module that leaks its internals is not modular.

A shared utility that needs flags to handle different concepts is probably wrong.

Do not report more than 3 findings unless asked.

Do not recommend anything that cannot fit in a small PR.

End with the one fix you would do first.
