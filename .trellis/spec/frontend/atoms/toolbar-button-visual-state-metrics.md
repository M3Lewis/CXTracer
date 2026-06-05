---
id: frontend.controls.toolbar-button-visual-state-metrics
type: pitfall
priority: should
applies_when:
  - styling toolbar Button or ToggleButton controls in AXAML
  - overriding SukiUI button templates
  - fixing hover, pressed, checked, or border visual states
code_anchors:
  - src/CodexLens/App.axaml
  - src/CodexLens/Views/MainWindow.axaml
  - src/CodexLens/Views/SettingsWindow.axaml
verify:
  - pointer hover keeps the button border visible
  - hover, pressed, and checked states do not change button size
  - toolbar buttons keep the same visual bounds as the pre-change SukiUI baseline
  - toolbar buttons in rows with taller neighboring content are vertically centered instead of stretching to the row height
  - Button and ToggleButton toolbar variants both keep their template border visible on hover
  - similar controls outside the target group, such as transcript pane arrow buttons, are not changed
  - shared toolbar button styles used by multiple windows are defined once and reused by class
source:
  kind: human_confirmed
  ref: user-reported toolbar hover border and size regression on 2026-06-04
last_checked: 2026-06-04
---

# Rule

When fixing toolbar button visual states, preserve the control's baseline metrics at the same time as the border and background states.

# Why

A previous hover-border fix replaced the SukiUI button template for text toolbar buttons. The border no longer disappeared on pointer hover, but the buttons changed size because the replacement style invented its own `Padding`, `MinHeight`, and then `MinWidth`. After those guesses were removed, transcript-toolbar buttons still rendered too tall because they sat in a grid row whose left side had two lines of text and the buttons were stretching to the row height. A later regression showed `ToggleButton` hover remained correct while plain `Button` hover lost the border, so hover fixes must cover both control types and the template chrome that renders the border. Another attempted fix also touched nearby circular pane arrow buttons that were not part of the user's target.

# Do

Scope toolbar text buttons through an explicit class and keep hover/pressed/checked border setters on that class. If replacing the template is unavoidable, inherit the theme's existing metrics by leaving `Padding`, `MinHeight`, and `MinWidth` unset unless a measured baseline proves they must be set. In mixed-height toolbar rows, set the target buttons to `VerticalAlignment="Center"` so their border does not stretch to neighboring multiline content. When a template-owned border renders the visible chrome, set hover/pressed/checked values on the named template child as well as on the owning Button or ToggleButton.

When the same toolbar button visual behavior is needed in more than one window, define the class style once at application scope and have each window opt in with `Classes="toolbarButton"`. Do not copy the template into each window.

# Do Not

Do not fix a text toolbar button issue by changing icon-only navigation buttons, and do not guess new `Padding`, `MinHeight`, or `MinWidth` values to compensate for a template override. Do not duplicate the `toolbarButton` template in a second window when the desired behavior is shared.
