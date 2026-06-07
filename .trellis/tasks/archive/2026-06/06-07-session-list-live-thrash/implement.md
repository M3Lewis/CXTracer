# Implementation Checklist - Session List Live Update Thrashing

- [ ] Modify `MainWindowViewModel.UpsertSession` to remove `Sessions.Clear()` and the full re-sort loop.
- [ ] Implement in-place index movement using `Sessions.RemoveAt` and `Sessions.Insert`.
- [ ] Build the project to verify compilation:
  ```bash
  dotnet build src/CXTracer/CXTracer.csproj --nologo -v q
  ```
