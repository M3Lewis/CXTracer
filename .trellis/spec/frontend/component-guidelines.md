# Components and Controls

The current UI is a dense desktop inspection tool, not a landing page or wizard. Screens should prioritize readable transcript data, stable panes, and direct controls.

## Current Composition

`MainWindow.axaml` uses:

- `Grid` for the overall shell and two-pane layout
- `Border` for grouped tool areas and event cards
- `TextBox` for root path and search
- `Button`, `ToggleButton`, `CheckBox`, `ComboBox`, and `ProgressBar` for controls
- `ListBox` for sessions
- `ItemsControl` inside `ScrollViewer` for event streams
- `GridSplitter` between conversation and execution panes
- `Expander` for raw events

`SettingsWindow.axaml` owns persistent app preferences that would clutter the main transcript toolbar. Main toolbar controls may open settings, but should not inline multi-step preference editors such as shortcut capture.

## Binding Pattern

Bind controls directly to `MainWindowViewModel` properties and generated commands:

```xml
<TextBox Text="{Binding RootPath, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
<Button Content="Refresh" Command="{Binding RefreshCommand}" />
<ComboBox ItemsSource="{Binding FilterOptions}" SelectedItem="{Binding SelectedFilter}" />
```

Use typed data templates for item models:

```xml
<DataTemplate x:DataType="models:DisplayEvent">
    <TextBlock Text="{Binding Title}" />
</DataTemplate>
```

## View-Only Events

Click handlers are acceptable only for behavior that needs rendered controls, such as the current up/down scroll buttons:

- `ConversationUp_Click`
- `ConversationDown_Click`
- `ExecutionUp_Click`
- `ExecutionDown_Click`
- `Settings_Click` to create/show the settings window owned by the main window

Settings-window key capture is also view-only: the window receives the next physical key event and forwards normalized modifier/key data to its ViewModel. The ViewModel owns validation and persistence.

When binding `CheckBox.IsChecked` (which is `bool?` in Avalonia) to a ViewModel property, always set `IsThreeState="False"` explicitly. SukiUI themes may override the default and turning on three-state causes clicks to cycle through `null`, which the ViewModel setter must handle or the checkbox will appear stuck.

```xml
<CheckBox IsThreeState="False"
          IsChecked="{Binding MyProperty, Mode=TwoWay}" />
```

When capturing shortcuts, modifier-only keydown events (`LeftCtrl`, `RightCtrl`, `LeftShift`, `RightShift`, `LeftAlt`, `RightAlt`) must keep capture mode active. A shortcut capture should complete only after receiving a valid final key such as a letter, digit, punctuation key, function key, or navigation key with at least one of `Ctrl`, `Shift`, or `Alt`. Do not hard-code shortcut validation to letters only; punctuation such as `Ctrl+Shift+'` is a valid user shortcut.

If multiple windows can capture or match the same shortcut, put physical-key normalization in one shared view-only helper so Settings and MainWindow cannot drift.

Pane-navigation buttons and keyboard navigation must share navigation state. The active transcript pane and current message are ViewModel state; code-behind may compute visual anchors and scroll offsets, but it must write the navigated `DisplayEvent` back to the ViewModel so mouse clicks and arrow keys continue from the same place.

Do not set local `BorderBrush` or `BorderThickness` values on controls whose active state is controlled by class styles. Local values override style setters and make active pane borders appear broken even when `Classes.active` is changing.

Command handlers should remain in the ViewModel for application behavior.

## Visual Style

- Keep the UI dense and operational.
- Prefer stable grid dimensions and predictable panes.
- Use card-like borders for repeated session/event items.
- Keep small status badges compact: use tight vertical padding, align them to the top of their grid row, and do not let them stretch to the height of adjacent multiline text.
- Reuse icon resources from `Icons.axaml` instead of duplicating path data.
- Active transcript pane and current message indicators must be visually obvious without shifting surrounding layout.

## Avoid

- Adding `SukiSideMenu` unless the app gains real navigation pages.
- Moving filter/search/refresh behavior into code-behind.
- Creating custom controls for one-off layout fragments.
- Adding decorative backgrounds that reduce transcript readability.
