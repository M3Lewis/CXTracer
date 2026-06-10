---
id: frontend.rendering.thread-safe-brush-rendering
type: pitfall
priority: must
applies_when:
  - creating or modifying models or viewmodels exposing brushes
  - instantiating brushes on background threads or in static constructors
code_anchors:
  - src/CXTracer/Models/DisplayEvent.cs
verify:
  - Check that no mutable SolidColorBrush is instantiated in classes parsed or initialized off the UI thread
  - Check that all static or background-constructed brushes use ImmutableSolidColorBrush
source:
  kind: bug_incident
  ref: task-2026-06-10-dispatcher-exception
last_checked: 2026-06-10
---

# Rule

Brushes defined statically or instantiated on background threads (e.g. inside model constructors or parsers executed in threadpool tasks) must be `ImmutableSolidColorBrush` from the `Avalonia.Media.Immutable` namespace, rather than standard `SolidColorBrush`.

# Why

Standard `SolidColorBrush` inherits from `AvaloniaObject` which has thread affinity to the thread it was created on. When models are parsed or initialized on background threads (like log scanning using `Task.Run` or `.ConfigureAwait(false)`), static constructors or constructors executing on the background thread will bind standard `SolidColorBrush` instances to that background thread. Later, when the UI thread tries to access and render using those brushes, Avalonia throws an `InvalidOperationException`: "The calling thread cannot access this object because a different thread owns it."

`ImmutableSolidColorBrush` is thread-safe and lacks thread affinity, making it safe to instantiate on background threads and bind to UI elements.

# How to Reference

Import the namespace:
```csharp
using Avalonia.Media;
using Avalonia.Media.Immutable;
```

And define thread-safe brushes:
```csharp
private static readonly IBrush BgUser = new ImmutableSolidColorBrush(Color.Parse("#EAF7F0"));
```
