# Implementation Plan - Enhance UI Features

This document provides a step-by-step checklist to execute, verify, and validate the UI enhancements for images, collapsible tools, and colored diffs.

## 1. Pre-Implementation Check
- [ ] Working tree is clean (`git status`).
- [ ] Build compiles cleanly (`dotnet build`).

## 2. Step-by-Step Implementation

### Step 2.1: Model & Enum Changes
- [ ] Add `ToolCall` and `ToolResult` to `EventKind.cs`.
- [ ] Create `DiffLine` model containing `Text`, `Foreground`, and `Background` properties.
- [ ] Update `DisplayEvent.cs` to add:
  - `IsExpanded` and `CanExpand` fields and properties.
  - `ImagePath` and `HasImage` properties.
  - `DisplayText` and `PreviewText` computed strings.
  - `AdditionsCount`, `DeletionsCount` and `DiffLines` collection.
  - `ToggleExpandCommand` to flip `IsExpanded`.
  - Constructor/Initialization logic setting the hybrid expansion state rules (`REQ-2.7`, including `Error` event folding).
  - Add `ResetExpansionState(bool expandAllByDefault)` method.

### Step 2.2: Event Parser & Path Resolution
- [ ] Update `CodexEventParser.cs` to distinguish `ToolCall` and `ToolResult` inside `Classify()`.
- [ ] Update `BuildTitle()` and `BuildText()` in `CodexEventParser.cs` to handle the new kinds.
- [ ] Update `ParseLine()` signature to accept `sessionFilePath` and implement image regex extraction (markdown, HTML, standalone extension paths).
- [ ] Implement path resolution (user home `~`, relative directory mapping, `/mnt/` drive conversion, and WSL UNC path resolution).
- [ ] Update references in `SessionReader.cs` and `SessionScanner.cs` to pass the `filePath` to `ParseLine()`.

### Step 2.3: Value Converter
- [ ] Implement `ImagePathToBitmapConverter.cs` in `src/CXTracer/Converters/`.
- [ ] Add base64 data URL decoding and local file stream loading with try-catch logic.

### Step 2.4: View Styling, UI Controls & Settings
- [ ] Register `ImagePathToBitmapConverter` in `MainWindow.axaml` resources.
- [ ] Update event item templates in `MainWindow.axaml` (left & right ListBoxes):
  - Add additions/deletions badge counts for Diffs in the header.
  - Add expand/collapse toggle buttons (visible on `CanExpand`).
  - Add image display control (visible on `HasImage`).
  - Add line-by-line colored diff rendering using `ItemsControl` (visible on `IsDiff && IsExpanded`).
  - Toggle visibility of standard `TextBlock` vs colored `ItemsControl` based on expansion state.
- [ ] Add `ExpandAllEventsByDefault` to `AppSettings.cs` and implement serialization/deserialization.
- [ ] Wire up `ExpandAllEventsByDefault` in `MainWindowViewModel` (load, save, on-change handler updating all active events) and `SettingsWindowViewModel`.
- [ ] Bind checkbox for `ExpandAllEventsByDefault` in `SettingsWindow.axaml`.
- [ ] Add `ExpandAllEvents` localized resource strings in `en-US.axaml` and `zh-CN.axaml`.

## 3. Validation and Verification

### 3.1 Validation Commands
- Run build to verify compilation:
  ```powershell
  dotnet build
  ```
- Run the app and load `samples/sample-rollout.jsonl` or real session logs containing tool calls, diffs, and image paths:
  ```powershell
  dotnet run --project .\src\CXTracer\CXTracer.csproj
  ```

### 3.2 Verification Matrix
- [ ] **AOT compatibility**: Verify no warnings or compilation issues.
- [ ] **Image Rendering**: Check if markdown `![caption](path)` and base64 URLs display the image correctly in cards.
- [ ] **Collapsible Tools**: Check if tool calls and outputs default to collapsed if long, and expand on toggle button click.
- [ ] **Colored Diffs**: Verify diff lines are styled green (additions) and red (deletions), and the badge shows correct counts in the header.
- [ ] **Persistent Expand Option**: Verify the checkbox toggles expansion of all messages, saves/loads correctly, and updates loaded events immediately.

## 4. Risky Files & Rollback Points
- `src/CXTracer/Models/DisplayEvent.cs` (Core model)
- `src/CXTracer/Services/CodexEventParser.cs` (Event parsing)
- `src/CXTracer/Views/MainWindow.axaml` (Main layout)
- `src/CXTracer/Services/AppSettings.cs` (Settings schema)
- `src/CXTracer/Views/SettingsWindow.axaml` (Settings layout)
