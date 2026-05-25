# Backend Directory Structure

Recommended solution organization for a desktop application with clean separations between business logic, persistence, and UI composition.

## Standard Structure

```text
src/
├── MyApp.Domain/
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Enums/
│   ├── Interfaces/
│   └── Exceptions/
├── MyApp.Application/
│   ├── Services/
│   ├── UseCases/
│   ├── DTOs/
│   ├── Commands/
│   ├── Queries/
│   └── Abstractions/
├── MyApp.Infrastructure/
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   ├── Configurations/
│   │   ├── Migrations/
│   │   └── Repositories/
│   ├── Logging/
│   ├── Files/
│   ├── Http/
│   └── DependencyInjection.cs
└── MyApp.Desktop/
    ├── App.axaml.cs
    ├── Program.cs
    ├── Views/
    ├── ViewModels/
    ├── Services/
    └── DependencyInjection.cs
```

## Layer Responsibilities

- `Domain`: core business rules, entities, value objects, and contracts
- `Application`: use-case orchestration and business workflows
- `Infrastructure`: EF Core, file I/O, external APIs, and concrete implementations
- `Desktop`: Avalonia/SukiUI shell, ViewModels, and composition root wiring

## Dependency Direction

Allowed dependency flow:

```text
Desktop -> Application -> Domain
Desktop -> Infrastructure -> Domain
Application -> Domain
Infrastructure -> Domain
```

Not allowed:

- `Domain -> Infrastructure`
- `Domain -> Desktop`
- `Application -> Desktop`

## Composition Roots

Keep registration code close to the layer that owns the implementation.

- `Infrastructure/DependencyInjection.cs` registers repositories, persistence, and integration services
- `Desktop/DependencyInjection.cs` registers ViewModels, window services, and shell-level UI adapters
- `App.axaml.cs` or startup code composes the full container

## Naming Conventions

- interfaces use `I*`
- repository implementations end with `Repository`
- application services end with `Service`
- use-case handlers should describe the business action, not the transport

## Forbidden Patterns

- putting EF Core entities in the desktop project
- placing ViewModels in application or infrastructure projects
- mixing composition root code with business service implementations
- flat solutions where every service, repository, and entity lives in one folder
