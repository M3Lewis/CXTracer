# Native AOT Publish Blocker

## Status

Implementation reached Native AOT publish, but publish is blocked by a missing Windows native toolchain.

## Command

```powershell
dotnet publish src\CodexLens\CodexLens.csproj -c Release -r win-x64 /p:PublishAot=true
```

## Result

Restore and managed build completed, then Native AOT stopped at the platform linker step:

```text
Platform linker not found. Ensure you have all the required prerequisites documented at https://aka.ms/nativeaot-prerequisites, in particular the Desktop Development for C++ workload in Visual Studio. For ARM64 development also install C++ ARM64 build tools.
```

Retested on 2026-06-04 with the same command and the result is unchanged. The current blocker is still the missing Windows platform linker, not C# compile errors or app-code AOT warnings.

## Boundary Decision

Installing Visual Studio Build Tools, Desktop Development for C++, or Windows SDK components is out of scope for this task. The user must install the missing Native AOT prerequisites before publish and runtime smoke testing can continue.

## Work Completed Before Blocker

- Added Release-scoped Native AOT publish settings to `src/CodexLens/CodexLens.csproj`.
- Added source-generated JSON metadata in `src/CodexLens/Services/AppJsonContext.cs`.
- Updated `AppSettingsService` to load/save settings through source-generated JSON metadata.
- Updated `CodexEventParser.PrettyCompact` to serialize `JsonElement` through source-generated JSON metadata.
- Confirmed existing AXAML data templates already have explicit `x:DataType`.

## Verified

```powershell
dotnet build .\CodexLens.sln --no-restore
```

Passed with 0 warnings and 0 errors after the code changes.
