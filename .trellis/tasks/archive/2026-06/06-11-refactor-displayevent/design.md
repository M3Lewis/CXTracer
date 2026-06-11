# Design Document

## Proposed Solution
We will split the `DisplayEvent` class using C#'s `partial class` feature. This keeps all properties and methods within the same semantic class but organizes the source code files by concern.

### File Organization
1. **`DisplayEvent.cs`**:
   - Class properties (Id, LineNumber, Text, etc.).
   - Observable properties (`IsExpanded`, `CanExpand`, `ImagePath`, `DiffLines`).
   - Initialization and expansion control methods (`Initialize`, `ResetExpansionState`, `ToggleExpandCommand`).
   - Diff parsing logic (`ParseDiffLines`).
   - Navigation target modification callback (`OnIsCurrentNavigationTargetChanged`).
2. **`DisplayEvent.Styling.cs`**:
   - Background, border, and badge brushes (`BgUser`, `BdrUser`, etc.).
   - Computed styling properties (`CardBackground`, `CardBorder`, `CardBorderThickness`, `RoleBadgeBackground`, `RoleBadgeForeground`, `RoleLabel`).
3. **`DisplayEvent.Images.cs`**:
   - Static regex patterns (`MarkdownImageRegex`, `HtmlImageRegex`).
   - Path/json image extraction methods (`ExtractImagePath`, `ExtractImagePathFromJson`, `FindImageRecursively`, `FindImageInContentArray`).
   - Path translation and resolution (`ResolvePath`, `ResolvePlaceholderImages`).

## Risks & Mitigations
- **Namespace Issues**: Keep all files in the `CXTracer.Models` namespace.
- **Using Directives**: Ensure required namespaces (`System.IO`, `System.Text.Json`, `System.Text.RegularExpressions`, `Avalonia.Media`, etc.) are imported in the files that use them.
