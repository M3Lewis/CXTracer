# Implementation Checklist - Search Redesign

- [ ] Modify `MainWindowViewModel.cs` to add `_allSessions`, `SessionSearchText`, `EventSearchText`, and filtering logic.
- [ ] Modify `MainWindow.axaml` to update the placeholders and layout columns, inserting the new Event search text box.
- [ ] Build the project to verify compilation:
  ```bash
  dotnet build src/CXTracer/CXTracer.csproj --nologo -v q
  ```
