# Implementation Checklist

## Step 1: Add Focusable="False" to ListBoxItem style

- [ ] In `MainWindow.axaml`, under `<Style Selector="ListBox.sessionList ListBoxItem">`, add:
  ```xml
  <Setter Property="Focusable" Value="False" />
  ```

**File**: `src/CXTracer/Views/MainWindow.axaml`

## Step 2: Add Focusable="False" to Session ListBox

- [ ] In `MainWindow.axaml`, under `<ListBox Grid.Row="2" Classes="sessionList" ...>`, add:
  ```xml
  Focusable="False"
  ```

**File**: `src/CXTracer/Views/MainWindow.axaml`

## Validation

- Build the project:
  ```bash
  dotnet build src/CXTracer/CXTracer.csproj --nologo -v q
  ```
- Run the application.
- Click a session in the session list.
- Verify the session list item is selected.
- Press Up/Down arrow keys.
- Verify the active transcript pane is navigated, and the selected session does NOT change.
