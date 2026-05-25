# .NET Backend Guidelines

Production backend guidelines for desktop applications built with .NET 8, Clean Architecture, Entity Framework Core, and dependency injection.

## Structure

### [Directory Structure](./directory-structure.md)

Recommended solution layout for Domain, Application, Infrastructure, and Desktop composition roots.

### [Dependency Injection](./dependency-injection.md)

How to register services, control lifetimes, and avoid service locator patterns in business code.

### [Service Boundaries](./service-boundaries.md)

Rules for separating domain logic, application orchestration, infrastructure, and UI-facing adapters.

### [Database Guidelines](./database-guidelines.md)

EF Core, SQLite-friendly persistence patterns, repository boundaries, and migration rules.

### [Error Handling](./error-handling.md)

Expected failure modeling, exception propagation, and user-facing error mapping.

### [Logging Guidelines](./logging-guidelines.md)

Structured logging conventions for services, persistence, startup, and background work.

### [Quality Checklist](./quality-guidelines.md)

Review checklist for async behavior, nullability, layering, tests, and forbidden backend patterns.

## Tech Stack

- **Runtime**: .NET 8
- **Persistence**: EF Core with SQLite-friendly patterns
- **DI**: Microsoft.Extensions.DependencyInjection
- **Logging**: Serilog or `ILogger<T>`-compatible structured logging
- **Architecture**: Clean Architecture / layered desktop application

## Usage

These guidelines are intended to be used as:

1. **Project bootstrap rules** for new .NET desktop solutions
2. **Implementation reference** for services, persistence, and composition roots
3. **Code review checklist** for layering, async correctness, and logging
4. **Onboarding material** for engineers new to this project shape

## Core Rules

- Domain and application logic must not depend on Avalonia or SukiUI types.
- `DbContext` and EF entities belong to Infrastructure, not ViewModels.
- `IServiceProvider` access is restricted to composition roots and framework boundary adapters.
- Repositories and services return materialized results, not `IQueryable`.
- All I/O and database work should be asynchronous unless a library makes that impossible.
