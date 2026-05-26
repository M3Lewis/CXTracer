# Bug Analysis: Settings Window Toggle And Shortcut Capture

## 1. Root Cause Category

- **Category**: E - Implicit Assumption
- **Specific Cause**: The settings proxy ViewModel exposed a non-nullable `bool` for an Avalonia `CheckBox.IsChecked` binding even though the control contract is nullable. Shortcut capture also assumed every keydown during capture was either a complete shortcut or an invalid shortcut, but modifier keys are intermediate physical key events and must not end capture.

## 2. Why Fixes Failed

1. Initial settings-window implementation validated the happy path only through compilation. It did not manually exercise checkbox toggle state or modifier-only keydown events.
2. The shortcut capture handler treated non-letter keys as terminal invalid input. That is correct for keys like `Escape`, but wrong for standalone modifier keys used while forming a combination.

## 3. Prevention Mechanisms

| Priority | Mechanism | Specific Action | Status |
|----------|-----------|-----------------|--------|
| P0 | Documentation | Document modifier-only keydown behavior for shortcut capture in frontend component guidelines. | DONE |
| P0 | Documentation | Document nullable proxy properties for Avalonia nullable control contracts. | DONE |
| P0 | Quality checklist | Add verification item for modifier-only key presses during shortcut capture. | DONE |
| P1 | Test coverage | Add UI/integration coverage for SettingsWindow toggle and shortcut capture when the project gains UI test infrastructure. | TODO |

## 4. Systematic Expansion

- **Similar Issues**: Any settings-window proxy property bound to Avalonia nullable control state can flash or fail if exposed as a stricter non-nullable type without translation.
- **Design Improvement**: Shortcut capture code should distinguish intermediate modifier events from terminal invalid keys before delegating validation to ViewModel state.
- **Process Improvement**: UI changes involving keyboard capture require manual verification of physical key sequences, not just final chord values.

## 5. Knowledge Capture

- [x] Updated `.trellis/spec/frontend/component-guidelines.md`.
- [x] Updated `.trellis/spec/frontend/state-management.md`.
- [x] Updated `.trellis/spec/frontend/quality-guidelines.md`.
- [x] Checked for `src/templates/markdown/spec/`; no such template tree exists in this repo.
