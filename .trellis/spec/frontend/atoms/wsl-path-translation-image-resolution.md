---
id: frontend.rendering.wsl-path-translation-image-resolution
type: compatibility
priority: must
applies_when:
  - parsing event logs containing paths
  - resolving relative, home, or unix paths to windows/wsl file paths
code_anchors:
  - src/CXTracer/Models/DisplayEvent.cs
verify:
  - Check that home path (~) maps to Environment.SpecialFolder.UserProfile
  - Check that unix /mnt/ paths map to Windows drive letters
  - Check that absolute unix paths on WSL translate to WSL UNC format
source:
  kind: human_confirmed
  ref: task-2026-06-10-wsl-image-paths
last_checked: 2026-06-10
---

# Rule

Image path resolution logic for event logs must dynamically translate relative paths, Unix `~` paths, and WSL absolute paths to their Windows equivalents to guarantee image rendering on Windows.

## Requirements

1. **User Home Expansion**: Convert `~` to the Windows user profile path (`Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)`).
2. **WSL Mounts (`/mnt/`)**: Translate `/mnt/<drive>/...` paths (e.g. `/mnt/c/path`) to Windows drive paths (e.g. `C:\path`).
3. **WSL Absolute Paths (`/...`)**: Translate Unix absolute paths to WSL UNC paths (e.g. `\\wsl.localhost\<distro>\...` or `\\wsl$\<distro>\...`) based on the session file's parent folder path when the session file is hosted on a WSL share.

# Why

Developers run coding agents or tool executions in WSL/Linux environments while using CXTracer as a Windows desktop GUI. Logs generated in WSL contain Unix absolute and mount paths, which standard Windows file APIs cannot read directly. Without this translation, images fail to load in the GUI.
