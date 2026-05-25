# Components & Controls

These guidelines define how desktop screens should be composed with Avalonia and SukiUI controls.

## View Composition

- Use `SukiWindow` for the application shell.
- Use `UserControl` for pages, panels, and reusable view fragments.
- Keep code-behind minimal; layout, resources, and bindings belong in AXAML.
- Give each page a dedicated ViewModel with a compiled binding context.

## Navigation

`SukiSideMenu` is the preferred primary navigation control for desktop applications.

```xml
<suki:SukiSideMenu IsSearchEnabled="True">
    <suki:SukiSideMenu.Items>
        <suki:SukiSideMenuItem Header="Dashboard" Classes="Compact">
            <suki:SukiSideMenuItem.Icon>
                <!-- icon -->
            </suki:SukiSideMenuItem.Icon>
            <suki:SukiSideMenuItem.PageContent>
                <views:DashboardView />
            </suki:SukiSideMenuItem.PageContent>
        </suki:SukiSideMenuItem>
    </suki:SukiSideMenu.Items>
</suki:SukiSideMenu>
```

## SukiSideMenu Rules

- Every `SukiSideMenuItem` must define `PageContent`.
- Use `HeaderContent` for user or workspace context.
- Use `FooterContent` for settings, account, or low-frequency actions.
- Keep the shell ViewModel responsible for current page state and menu item selection.

SukiUI documents that missing `PageContent` can trigger runtime visual tree exceptions. Treat it as required.

## Control Selection

Prefer SukiUI controls when they cover the use case directly:

- `SukiSideMenu` for shell navigation
- `SettingsLayout` for dense preference screens
- `InfoBar` for inline status that should remain visible in the page
- dialog host for blocking confirmation or structured interaction
- toast host for transient feedback

Use plain Avalonia controls when:

- SukiUI does not provide a stronger option
- the screen needs basic primitives like `Grid`, `StackPanel`, `ContentControl`, `ItemsControl`, or `Border`

## Reusable Controls

Create reusable desktop controls when:

- the same AXAML fragment appears in three or more places
- the interaction pattern is repeated across multiple screens
- the control carries local styling that should remain consistent

Keep reusable controls narrow. A control should either:

- present one concept clearly, or
- package one interaction pattern cleanly

## Layout Guidance

- Use `Grid` for structured page layout.
- Use `StackPanel` only for simple linear groups.
- Keep toolbar actions compact and aligned with desktop density expectations.
- Avoid giant empty hero layouts; desktop tools should surface usable content immediately.

## Forbidden Patterns

- Navigation pages implemented as code-behind-only views
- `SukiSideMenuItem` without `PageContent`
- Business logic embedded in converters or view code-behind
- Custom styled clones of controls that SukiUI already provides
