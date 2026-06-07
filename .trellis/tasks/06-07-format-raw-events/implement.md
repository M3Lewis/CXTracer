# Implementation: Format Raw Events Display

## Checklist
- [ ] Add `FormattedRawJson` property with backing field memoization cache to `DisplayEvent.cs`.
- [ ] Modify bottom `ItemsControl` template in `MainWindow.axaml` to render each raw event in a card-like layout, bind to `FormattedRawJson`, and add `PointerPressed="CardBorder_PointerPressed"`.
- [ ] Update detail popup panel in `MainWindow.axaml` to bind its raw expander text block to `DetailPopupEvent.FormattedRawJson`.
- [ ] Update `CardBorder_PointerPressed` in `MainWindow.axaml.cs` to copy `RawJson` instead of `Text` when right-clicking a raw event card.
- [ ] Run `dotnet build` to verify compilation.

## Verification Command
```powershell
dotnet build src/CXTracer/CXTracer.csproj --nologo -v q
```
