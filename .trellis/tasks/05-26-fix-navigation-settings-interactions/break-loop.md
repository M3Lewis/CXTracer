# Bug Analysis: Settings Toggle, Shortcut Capture, And Scroll Navigation

## 1. Root Cause Category

- **Category**: B/E - Cross-Layer Contract + Implicit Assumption
- **Specific Cause**: Shortcut capture had three different contracts: the PRD/spec said modifier-plus-letter, the user expected modifier-plus-key, and the model only validated one letter. Settings and MainWindow duplicated physical-key normalization, so non-letter shortcuts could never become valid or enable OK. The settings checkbox also needed to satisfy Avalonia's nullable `CheckBox.IsChecked` contract while avoiding re-entrant proxy updates. Scroll buttons, keyboard arrows, active pane borders, and current message highlighting did not share one navigation state. Pane active styling was additionally hidden by local `BorderBrush` / `BorderThickness` values that overrode active class styles.

## 2. Why Fixes Failed

1. **Incomplete Scope**: The previous fix made modifier-only keydown non-terminal but left `ShortcutGesture.IsValid`, parse, matching, and status text letter-only.
2. **Spec Drift**: The task PRD encoded the wrong acceptance criterion (`modifier + letter`) and therefore did not protect the user-required `Ctrl+Shift+'` flow.
3. **Duplicated View Logic**: SettingsWindow and MainWindow each had their own key-to-letter helper, making it easy to fix one path without fixing all shortcut capture/match paths.
4. **State Split**: Mouse buttons used the pane named by the clicked button, while keyboard arrows used `ActiveTranscriptPane`; synchronized navigation therefore diverged after the target moved to the other pane.
5. **Style Precedence**: Active pane border styles existed, but local border values on the same controls had higher precedence.

## 3. Prevention Mechanisms

| Priority | Mechanism | Specific Action | Status |
|----------|-----------|-----------------|--------|
| P0 | Architecture | Centralize physical-key normalization for shortcut capture/match in a shared view helper. | DONE |
| P0 | Documentation | Document modifier-plus-key behavior, including punctuation shortcuts, in frontend component guidelines. | DONE |
| P0 | Documentation | Document nullable `CheckBox.IsChecked` proxy properties with local field synchronization. | DONE |
| P0 | Quality checklist | Add verification for modifier-only, modifier-plus-letter, modifier-plus-punctuation, and rapid scroll-button clicks. | DONE |
| P0 | State ownership | Store current navigated message in ViewModel state and use it for both mouse and keyboard navigation. | DONE |
| P0 | Quality checklist | Add verification for active pane and current message visual highlighting. | DONE |
| P1 | Test coverage | Add UI/integration coverage for SettingsWindow toggle and shortcut capture when the project gains UI test infrastructure. | TODO |

## 4. Systematic Expansion

- **Similar Issues**: Any shortcut or hotkey feature that validates only display text can reject physically valid keys after view-level capture has already succeeded.
- **Design Improvement**: Keep Avalonia `Key` handling and scroll-anchor discovery in view-only helpers, but keep selected pane/message navigation state in the ViewModel so all input methods share it.
- **Process Improvement**: UI changes involving keyboard capture and scroll navigation require manual verification of physical key sequences and repeated input timing, not just build success.

## 5. Knowledge Capture

- [x] Updated `.trellis/spec/frontend/component-guidelines.md`.
- [x] Updated `.trellis/spec/frontend/state-management.md`.
- [x] Updated `.trellis/spec/frontend/quality-guidelines.md`.
- [x] Checked for `src/templates/markdown/spec/`; no such template tree exists in this repo.
