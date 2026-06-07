# Implementation Checklist

## 1. ViewModel state
- [ ] Add `DetailPopupEvent` observable property to `MainWindowViewModel`
- [ ] Add `IsDetailPopupOpen` computed property
- [ ] Add `ShowDetailPopup(DisplayEvent)` / `CloseDetailPopup()` methods

## 2. AXAML overlay
- [ ] Add overlay `Panel` as last child of root Grid (spans all rows/columns)
- [ ] Backdrop: `Border` with `#80000000` background, click handler
- [ ] Content panel: centered `Border` with `ScrollViewer`, header, text, RawJson expander
- [ ] Bind visibility to `IsDetailPopupOpen`, content to `DetailPopupEvent`

## 3. Click handler on cards
- [ ] Add `PointerPressed` handler on card `Border` in Conversation DataTemplate
- [ ] Add same handler on card `Border` in Execution DataTemplate
- [ ] Handler calls `viewModel.ShowDetailPopup(displayEvent)`

## 4. Dismiss behavior
- [ ] Backdrop click → `CloseDetailPopup()`
- [ ] Escape in `Window_KeyDown` → `CloseDetailPopup()` (before other key handling)
- [ ] × button in popup header → `CloseDetailPopup()`
- [ ] Block Up/Down/Left/Right while popup is open

## 5. Verification
- [ ] `dotnet build` passes
- [ ] Manual: click card → popup opens with correct content
- [ ] Manual: Escape/backdrop/× all dismiss
- [ ] Manual: Up/Down do nothing while popup is open
- [ ] Manual: popup shows RawJson expander that works
