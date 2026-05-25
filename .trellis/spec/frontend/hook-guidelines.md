# Behaviors & Attached Properties

Avalonia does not use React-style hooks. Reusable view logic should be expressed with bindings, commands, behaviors, or attached properties.

## Use the Smallest Tool That Fits

- use a **binding** when the UI only needs data projection
- use a **converter** for small value transformation
- use an **attached property** to extend control state or metadata
- use a **behavior** for declarative event-driven interaction
- use **code-behind** only for view-only glue that cannot reasonably be expressed another way

## Attached Properties

Use attached properties for lightweight control extensions.

```csharp
public static readonly AttachedProperty<bool> IsSpecialFocusedProperty =
    AvaloniaProperty.RegisterAttached<MyBehaviorHost, Control, bool>("IsSpecialFocused");
```

Good use cases:

- opt-in control state
- small styling toggles
- metadata that multiple styles or templates consume

## Behaviors

Use `Avalonia.Xaml.Behaviors` when a control needs reusable interaction logic without code-behind event handlers.

```xml
<Button Content="Open">
    <Interaction.Behaviors>
        <EventTriggerBehavior EventName="PointerEntered">
            <InvokeCommandAction Command="{Binding HoverCommand}" />
        </EventTriggerBehavior>
    </Interaction.Behaviors>
</Button>
```

Good use cases:

- event-to-command bridging
- pointer interaction
- focus and keyboard interaction
- small visual behaviors reused across screens

## Code-Behind Boundary

Code-behind should normally contain only `InitializeComponent()`.

Acceptable exceptions:

- wiring a view-only platform concern
- a framework integration that cannot be bound declaratively
- temporary debugging that is removed before merge

## Forbidden Patterns

- calling domain services directly from behaviors
- large feature logic in converters
- page-specific event handlers copied across multiple views
- treating code-behind as the default interaction model
