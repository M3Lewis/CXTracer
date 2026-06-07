# CXTracer Service Guidelines

Backend here means the local service/model code that reads Codex transcript files. This project is a single .NET 8 Avalonia desktop app, not a web backend and not an EF Core application.

## Structure

### [Directory Structure](./directory-structure.md)

Current source layout and where parser, reader, watcher, and model code belongs.

### [Service Boundaries](./service-boundaries.md)

How `Services/`, `Models/`, and `ViewModels/` divide filesystem, parsing, and presentation responsibilities.

### [Database Guidelines](./database-guidelines.md)

Persistence policy for this app: no database, no indexes, and read-only access to Codex session JSONL files.

### [Dependency Injection](./dependency-injection.md)

Current manual composition pattern in `App.axaml.cs` and when a DI container would be worth adding.

### [Error Handling](./error-handling.md)

How filesystem and malformed JSONL failures are handled without hiding real bugs.

### [Logging Guidelines](./logging-guidelines.md)

Current logging posture and where future diagnostics should go.

### [Quality Checklist](./quality-guidelines.md)

Review checklist for IO sharing, cancellation, parser tolerance, and read-only guarantees.

## Tech Stack

- .NET 8, nullable enabled, implicit usings enabled
- Avalonia 11.3.14 and SukiUI 6.1.1
- CommunityToolkit.Mvvm 8.4.0
- `System.Text.Json`, `FileStream`, `FileSystemWatcher`

## Core Rules

- Never write to, delete from, or launch anything inside the Codex CLI session tree.
- Read transcript files with `FileShare.ReadWrite | FileShare.Delete` so active Codex sessions can keep writing.
- Keep parser and filesystem work in `Services/`; keep mutable display state in `ViewModels/`; keep display records in `Models/`.
- Do not introduce EF Core, repositories, HTTP APIs, or a DI container unless a feature actually needs them.
- Prefer focused, readable service methods over generic infrastructure abstractions.
