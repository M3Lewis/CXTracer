# Design: Session List Key Navigation Separation

## Architecture

By default, Avalonia `ListBox` and `ListBoxItem` controls are focusable, allowing them to capture keyboard focus and key events. By explicitly setting `Focusable="False"` on both, we prevent them from ever capturing keyboard focus.

## Changes

1. **Session ListBox**:
   Add `Focusable="False"` in `MainWindow.axaml` at the declaration:
   ```xml
   <ListBox Grid.Row="2"
            Classes="sessionList"
            Focusable="False"
            ItemsSource="{Binding Sessions}"
            SelectedItem="{Binding SelectedSession, Mode=TwoWay}">
   ```

2. **Session ListBoxItem**:
   Add a style setter in `MainWindow.axaml` under `Style Selector="ListBox.sessionList ListBoxItem"`:
   ```xml
   <Style Selector="ListBox.sessionList ListBoxItem">
     <Setter Property="Focusable" Value="False" />
     <Setter Property="Padding" Value="0" />
     <Setter Property="Margin" Value="0" />
     ...
   </Style>
   ```

## Compatibility

- Mouse clicks will still select items.
- Focus will not be captured, so the window-level KeyDown handler will continue to intercept arrow keys.
