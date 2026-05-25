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

Command handlers should remain in the ViewModel for application behavior.

## Visual Style

- Keep the UI dense and operational.
- Prefer stable grid dimensions and predictable panes.
- Use card-like borders for repeated session/event items.
- Keep small status badges compact: use tight vertical padding, align them to the top of their grid row, and do not let them stretch to the height of adjacent multiline text.
- Reuse icon resources from `Icons.axaml` instead of duplicating path data.

## Avoid

- Adding `SukiSideMenu` unless the app gains real navigation pages.
- Moving filter/search/refresh behavior into code-behind.
- Creating custom controls for one-off layout fragments.
- Adding decorative backgrounds that reduce transcript readability.
