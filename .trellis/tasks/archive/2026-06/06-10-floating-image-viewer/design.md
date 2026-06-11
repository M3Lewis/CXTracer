# Design Document

## Proposed Solution

### 1. View Model & State
In `MainWindowViewModel.cs`, expose properties:
- `IsImageViewerOpen`
- `ViewerImagePath`
- `ViewerImageStretch`
- `ToggleImageSizeCommand`
- `CloseImageViewerCommand`
- `ShowImageViewerCommand`

### 2. View Layout
In `MainWindow.axaml`, add a modal overlay grid:
- `ZIndex="1100"`
- Contains a styled dark semi-transparent backdrop, close button, size toggle, and a `ScrollViewer` wrapped around the target `Image`.
- Bind `Image.Source` using the custom `ImagePathToBitmapConverter`.

### 3. Localization
Add resource dictionary strings for `ViewerFitWindow` and `ViewerOriginalSize` in `zh-CN.axaml` and `en-US.axaml`.
