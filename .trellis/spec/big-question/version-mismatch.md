# Avalonia and SukiUI Version Mismatch

## Symptom

Common setup failures include:

- `SukiWindow` not found
- `Unable to resolve type 'SukiTheme'`
- `MissingMethodException` during XAML load or startup

## Root Cause

Avalonia and SukiUI versions can drift out of compatibility, especially around beta releases or when mixing package and CI-built binaries.

The SukiUI launch guide explicitly calls this out as a common cause of startup failures.

## Recovery Steps

1. verify the installed Avalonia and SukiUI versions intentionally match your target setup
2. prefer a stable NuGet version first
3. if a required fix only exists in CI builds, switch to the matching GitHub Action build deliberately
4. retest `SukiTheme` resolution and `SukiWindow` startup immediately after the change

## Prevention

- upgrade Avalonia and SukiUI together, not independently
- record known-good version pairs in the project
- do a real startup smoke test after dependency upgrades
