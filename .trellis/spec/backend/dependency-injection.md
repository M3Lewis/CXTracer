# Dependency Injection

The project uses `Microsoft.Extensions.DependencyInjection` as the single service registration mechanism.

## Composition Root Rule

Build the container only at application startup.

Typical composition sequence:

1. infrastructure registrations
2. application registrations
3. desktop registrations
4. container build

## Lifetime Guidance

- **Singleton**: stateless services, app-wide coordinators, configuration providers
- **Transient**: ViewModels, short-lived handlers, per-operation services
- **Scoped**: use deliberately for units of work or explicit operation scopes; desktop apps do not get request scopes automatically

## Acceptable `IServiceProvider` Usage

`IServiceProvider` access is allowed only at framework boundaries such as:

- startup/bootstrap
- `ViewLocator`-style adapters
- factory abstractions that exist specifically to create runtime objects

## Forbidden Usage

- domain services calling `App.Services`
- repositories resolving dependencies ad hoc from the container
- ViewModels pulling arbitrary services from a static provider
- passing `IServiceProvider` through application logic as a hidden dependency

## Registration Style

Prefer explicit extension methods per layer:

```csharp
services
    .AddApplication()
    .AddInfrastructure(configuration)
    .AddDesktop();
```

This keeps the bootstrap readable and prevents startup logic from turning into an unstructured list.

## Desktop-Specific Note

If `ViewLocator` or a similar Avalonia adapter must resolve Views from DI, keep that exception narrow and document it. It is a framework bridge, not a license to use service location elsewhere.
