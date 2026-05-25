# Error Handling

Backend error handling should preserve intent, context, and stack traces.

## Classify Failures

Separate failures into two groups:

- **expected failures**: validation errors, missing records, rule violations
- **unexpected failures**: provider crashes, I/O failures, corrupted state, unknown exceptions

Expected failures should be modeled deliberately with domain exceptions, result types, or explicit error contracts.

## Exception Rules

- catch only exceptions you can handle or enrich
- use `throw;` when rethrowing to preserve the stack trace
- log unexpected failures once, at the boundary that owns the recovery decision

## User-Facing Mapping

Application and infrastructure layers may raise technical failures, but desktop-facing services must translate them into:

- meaningful dialog text
- actionable toast or status messages
- structured logs with enough context for diagnosis

## Good Pattern

```csharp
try
{
    await repository.SaveAsync(entity, cancellationToken);
}
catch (DbUpdateException ex)
{
    logger.LogError(ex, "Failed to persist settings for profile {ProfileId}", profileId);
    throw new SettingsPersistenceException(profileId, ex);
}
```

## Forbidden Patterns

- swallowing exceptions
- catching `Exception` and continuing silently
- throwing raw provider exceptions across every layer without translation
- user-facing messages built directly from exception text
