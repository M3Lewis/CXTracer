# Service Boundaries

This document defines what each backend layer may know and do.

## Domain

The Domain layer owns:

- entities
- value objects
- invariants
- domain exceptions
- business-facing interfaces

The Domain layer must not know:

- EF Core
- Avalonia or SukiUI
- serialization frameworks
- filesystem paths or HTTP clients

## Application

The Application layer owns:

- use-case orchestration
- transaction-level business workflows
- coordination between repositories and domain services
- DTOs that cross process or layer boundaries

Application may depend on domain contracts, but not on concrete infrastructure implementations.

## Infrastructure

The Infrastructure layer owns:

- `DbContext`
- repository implementations
- migrations and entity configurations
- file storage
- external service clients
- logging sinks and platform integrations

Infrastructure implements abstractions defined elsewhere. It should not become a second business layer.

## Desktop / Presentation

The Desktop layer owns:

- ViewModels
- user-triggered command orchestration
- shell-level adapters that bridge UI concerns to application services
- composition root code

The Desktop layer may call application services, but it must not implement persistence rules itself.

## Data Shape Rules

- persistence entities are not UI models
- application DTOs are not EF configuration objects
- ViewModels should project data for the screen instead of exposing infrastructure objects directly

## Forbidden Patterns

- ViewModel directly depending on `DbContext`
- repository returning EF tracking objects to the UI
- infrastructure service throwing raw provider errors without translation or context
- business rules duplicated separately in ViewModels and repositories
