# Backend Quality Checklist

Use this checklist during implementation and review.

## Layering

- domain and application code do not reference Avalonia or SukiUI
- infrastructure owns persistence and external integrations
- ViewModels do not depend on `DbContext` or repositories directly unless the architecture explicitly requires it

## Async and Cancellation

- I/O and database work is asynchronous
- public async methods accept `CancellationToken` when practical
- no sync-over-async in feature code

## Dependency Injection

- constructor injection is used for concrete dependencies
- no static service provider access in business logic
- lifetime choices are intentional and documented by code shape

## Persistence

- no `IQueryable` leaks out of infrastructure
- migrations exist for schema changes
- entity configuration is separated from domain rules

## Reliability

- exceptions are either handled meaningfully or allowed to fail fast
- logs are structured and contextual
- user-facing errors are translated at the desktop boundary

## Testing

- nullability is enabled
- application services have focused unit tests where logic is non-trivial
- repository behavior or mapping has integration coverage when it carries risk

## Forbidden Patterns

- `throw ex;`
- `catch (Exception) { }`
- EF Core usage in AXAML code-behind
- business logic spread across ViewModel, repository, and converter in parallel
