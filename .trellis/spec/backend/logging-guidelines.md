# Logging Guidelines

Use structured logging throughout the application. Prefer `ILogger<T>` abstractions even when Serilog is the underlying sink.

## Log Levels

- `Debug`: execution tracing useful during development
- `Information`: startup, shutdown, configuration load, major business events
- `Warning`: recoverable failures, retries, degraded behavior
- `Error`: operation failures that affect the user or correctness
- `Fatal`: process-terminating failures

## Message Template Rules

Use semantic templates, not string interpolation.

```csharp
logger.LogInformation("Loaded {Count} tracks for user {UserId}", count, userId);
```

Avoid:

```csharp
logger.LogInformation($"Loaded {count} tracks for user {userId}");
```

## What to Log

Good candidates:

- composition root and startup milestones
- persistence failures
- external integration failures
- retries, fallbacks, and degraded modes
- destructive or security-relevant user actions

## What Not to Log

- secrets, tokens, passwords, raw connection strings
- full payloads by default when they contain user or sensitive data
- duplicate logs for the same exception at every layer

## Boundary Rule

Lower layers log technical context. Upper layers log user-impact context. Do not spam both with the same event unless each adds materially different information.
