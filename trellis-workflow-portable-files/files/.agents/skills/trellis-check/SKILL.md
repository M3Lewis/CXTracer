---
name: trellis-check
description: "Comprehensive quality verification: spec compliance, stack-aware build/test checks, Verification Matrix evidence, cross-layer data flow, code reuse, and consistency checks. Use when code is written and needs behavior verification, before committing changes, or to catch context drift during long sessions."
---

# Code Quality Check

Comprehensive quality verification for recently written code. Combines spec compliance, stack-aware validation, cross-layer safety, and acceptance-criteria evidence.

Build, lint, type-check, or format passing is necessary but never sufficient. A task is complete only when every acceptance criterion and user-visible behavior change has verification evidence.

---

## Step 1: Identify What Changed

```bash
git diff --name-only HEAD
git status
```

## Step 2: Read Task Artifacts and Applicable Specs

Read the current task artifacts in order:

- `prd.md`
- `design.md` if present
- `implement.md` if present
- `research/implicit-rules.md` if present
- `.trellis/project-profile.md` if present

```bash
python ./.trellis/scripts/get_context.py --mode packages
```

For each changed package/layer, read the spec index and follow its **Quality Check** section:

```bash
cat .trellis/spec/<package>/<layer>/index.md
```

Read the specific guideline files referenced — the index is a pointer, not the goal.

## Step 3: Choose Stack-Aware Validation

Do not assume TypeScript / Node validation.

First read `.trellis/project-profile.md` or detect the project stack from repository files. Choose the narrowest commands that prove the changed behavior, then broaden when the change touches shared code, user-visible workflows, cross-layer contracts, or persistence.

For C# / .NET / Avalonia projects, prefer:

- `dotnet restore` when dependencies or project files changed
- `dotnet build --configuration Release --no-restore`
- `dotnet test --configuration Release --no-build`
- `dotnet format --verify-no-changes` when formatting or analyzer consistency matters

For Avalonia UI behavior, consider ViewModel unit tests, Avalonia headless tests, Appium, screenshot review, or documented manual verification when automation is not practical.

## Step 4: Run Project Checks

Run the selected validation commands. Fix any failures before proceeding.

## Step 5: Build Verification Matrix

Map every acceptance criterion and user-visible behavior change to evidence.

```md
## Verification Matrix

| Requirement / Acceptance Criteria | Verification Type | Evidence | Status |
|---|---|---|---|
| <criterion> | unit / integration / headless UI / existing test / manual | <test file, command, screenshot/manual note> | pass/fail/waived |
```

Evidence must be one of:

1. A new automated unit, integration, e2e, or headless test.
2. An existing automated test with exact command/name.
3. A documented manual verification with the reason automation is not practical.

If any acceptance criterion lacks evidence, add or update tests when practical. If automation is not practical, write the manual verification waiver clearly.

## Step 6: Review Against Checklist

### Code Quality

- [ ] Linter passes?
- [ ] Type checker passes (if applicable)?
- [ ] Build passes?
- [ ] Tests pass?
- [ ] Verification Matrix covers all acceptance criteria and user-visible behavior changes?
- [ ] No debug logging left in?
- [ ] No suppressed warnings or type-safety bypasses?

### Test Coverage

- [ ] New function → unit test added?
- [ ] Bug fix → regression test added?
- [ ] Changed behavior → existing tests updated?
- [ ] Avalonia UI behavior → ViewModel/headless UI/Appium/manual evidence selected appropriately?

### Spec Sync

- [ ] Does `.trellis/spec/` need updates? (new patterns, conventions, lessons learned)

> "If I fixed a bug or discovered something non-obvious, should I document it so future me won't hit the same issue?" → If YES, update the relevant spec doc.

## Step 7: Cross-Layer Dimensions (if applicable)

Skip this step if your change is confined to a single layer.

### A. Data Flow (changes touch 3+ layers)

- [ ] Read flow traces correctly: Storage → Service → API → UI
- [ ] Write flow traces correctly: UI → API → Service → Storage
- [ ] Types/schemas correctly passed between layers?
- [ ] Errors properly propagated to caller?

### B. Code Reuse (modifying constants, creating utilities)

- [ ] Searched for existing similar code before creating new?
  ```bash
  grep -r "pattern" src/
  ```
- [ ] If 2+ places define same value → extracted to shared constant?
- [ ] After batch modification, all occurrences updated?

### C. Import/Dependency (creating new files)

- [ ] Correct import paths (relative vs absolute)?
- [ ] No circular dependencies?

### D. Same-Layer Consistency

- [ ] Other places using the same concept are consistent?

---

## Step 8: Report and Fix

Report violations found and fix them directly. Re-run project checks after fixes. Include the Verification Matrix in the report or task notes.

If the failure cause is unclear, the user corrected the target, or the same issue repeats twice, stop the implementation loop and run `trellis-explore` before another edit.
