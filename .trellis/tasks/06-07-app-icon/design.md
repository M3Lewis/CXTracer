# Design: Add App Icon

## Data Flow & Reference Structure

```mermaid
graph TD
    PNG[Icons/AppIcon.png] -->|AvaloniaResource| Assembly[CXTracer Assembly]
    IconsAXAML[Icons/Icons.axaml] -->|ResourceInclude| AppAXAML[App.axaml]
    IconsAXAML -->|Defines| Key[AppIcon WindowIcon Resource]
    MainWindow[MainWindow.axaml] -->|References Key| Key
    SettingsWindow[SettingsWindow.axaml] -->|References Key| Key
```

- **Assembly Packaging**: `CXTracer.csproj` packages everything in the `Icons` folder via globbing (`<AvaloniaResource Include="Icons\**" />`). Thus, `/Icons/AppIcon.png` is embedded.
- **Resource Dictionary**: `AppIcon` is declared inside `Icons.axaml` as a `WindowIcon` which uses a TypeConverter to load `/Icons/AppIcon.png` from the assembly resource table.
