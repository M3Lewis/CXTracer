# Project Profile

## Stack

- Language: C#
- Runtime: .NET
- UI: Avalonia
- UI library: SukiUI
- Test framework: xUnit or NUnit, depending on the target test project

## Validation Commands

- Restore: `dotnet restore`
- Build: `dotnet build --configuration Release --no-restore`
- Test: `dotnet test --configuration Release --no-build`
- Format: `dotnet format --verify-no-changes`

Use the narrowest command that proves the touched behavior. For shared or user-visible changes, prefer restore/build/test over a compile-only check.

## Testing Strategy

- ViewModels: unit tests for commands, state transitions, validation, and async/reentrancy behavior.
- Services: unit or integration tests for business logic, IO boundaries, parsing, persistence, and error handling.
- Avalonia controls/windows: Avalonia headless tests for bindings, commands, input behavior, and control state when practical.
- Native dialogs / OS integration: Appium or documented manual verification when automation is not practical.
- Pure visual polish: screenshot or manual verification is acceptable only when the expected visual change and inspected surface are recorded.

## Completion Rule

Build, lint, type-check, or format passing is necessary but never sufficient. A task is complete only when every acceptance criterion and user-visible behavior change has verification evidence: automated test evidence, exact existing test/command evidence, or a documented manual verification waiver.
