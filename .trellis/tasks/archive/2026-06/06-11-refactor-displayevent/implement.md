# Implementation Plan

## Proposed Edits

### Models

#### [MODIFY] [DisplayEvent.cs](file:///k:/Code/ACTIVE/CXTracer/src/CXTracer/Models/DisplayEvent.cs)
- Keep only core properties, initialization flow, expansion controls, and diff line parsing.

#### [NEW] [DisplayEvent.Styling.cs](file:///k:/Code/ACTIVE/CXTracer/src/CXTracer/Models/DisplayEvent.Styling.cs)
- Extract all background, border, badge brushes, and styling getters.

#### [NEW] [DisplayEvent.Images.cs](file:///k:/Code/ACTIVE/CXTracer/src/CXTracer/Models/DisplayEvent.Images.cs)
- Extract image path extraction, JSON scanning, WSL translation, and session placeholder mapping.

## Verification
- Run `dotnet build` to ensure successful compilation.
