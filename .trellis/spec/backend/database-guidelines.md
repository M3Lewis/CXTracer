# Database Guidelines

Data persistence uses EF Core with local-first, desktop-friendly patterns.

## DbContext Placement

`DbContext` belongs in Infrastructure. It should not be referenced by Views or ViewModels.

## Entity Configuration

Prefer Fluent API via `IEntityTypeConfiguration<T>` to keep entity classes clean.

```csharp
public class UserSettings
{
    public int Id { get; set; }
    public string Theme { get; set; } = "Blue";
}

public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Theme).HasMaxLength(50);
    }
}
```

## Repository Rules

- materialize results inside repositories or application services
- return domain objects, DTOs, or concrete collections
- do not leak `IQueryable<T>` beyond infrastructure
- pass `CancellationToken` through async database calls when available

## Async Rules

Use asynchronous EF methods by default:

- `ToListAsync`
- `FirstOrDefaultAsync`
- `SingleAsync`
- `SaveChangesAsync`

Avoid sync-over-async data access in desktop commands.

## Migrations

- keep migrations in the infrastructure project
- create a migration for every schema change
- review generated migrations before committing
- never treat migrations as disposable generated noise

## Transactions

Use explicit transactions only when one logical operation spans multiple writes that must succeed or fail together.

Do not wrap every repository method in its own transaction by habit.

## Forbidden Patterns

- `DbContext` in ViewModels
- `IQueryable` returned to desktop code
- business logic encoded inside entity configuration classes
- unbounded table reads for UI screens that need paging or filtering
