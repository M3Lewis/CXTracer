# SukiSideMenuItem Missing PageContent

## Symptom

The application throws a runtime visual tree exception when rendering the side menu.

## Typical Error

`System.InvalidOperationException` complaining that `SukiSideMenuItem` already has a visual parent while being added elsewhere.

## Root Cause

SukiUI documents that each `SukiSideMenuItem` must provide `PageContent`. Omitting it can break the menu's internal presentation logic.

## Required Pattern

```xml
<suki:SukiSideMenuItem Header="Dashboard">
    <suki:SukiSideMenuItem.PageContent>
        <views:DashboardView />
    </suki:SukiSideMenuItem.PageContent>
</suki:SukiSideMenuItem>
```

## Prevention

- treat `PageContent` as required, not optional
- review side menu items after shell refactors
- prefer one stable pattern for navigation item declaration
