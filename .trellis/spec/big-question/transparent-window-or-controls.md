# Transparent Window or Controls

## Symptom

The window renders transparent, or many SukiUI controls appear transparent or visually broken.

## Root Cause

SukiUI requires `SukiTheme` to be registered in `App.axaml` with an explicit `ThemeColor`.

The official launch guide warns that if `ThemeColor` is not set, windows and many controls can become transparent.

## Required Fix

```xml
<Application.Styles>
    <suki:SukiTheme ThemeColor="Blue" />
</Application.Styles>
```

## Prevention

- never add `SukiTheme` without `ThemeColor`
- keep app bootstrap examples in sync with this rule
- verify the shell visually after any `App.axaml` refactor
