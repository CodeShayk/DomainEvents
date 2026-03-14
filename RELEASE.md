# Release Notes - v5.0.0

## New Architecture

```
Aggregate -> Interceptor -> Middleware -> Dispatcher -> Queue <- Listener -> Middleware -> Resolver -> Handler
```

### Components:

1. **IEventMiddleware** - Custom plugins that run before/after dispatch and handling
2. **IEventQueue** - In-flight non-persistent queue for events
3. **IEventListener** - Listens to queue and triggers handling

## New Features

### 1. Event Middleware

Custom plugins that run at various points in the event pipeline:

```csharp
public class MyMiddleware : IEventMiddleware
{
    public Task<bool> OnDispatchingAsync(EventContext context) { ... }
    public Task OnDispatchedAsync(EventContext context) { ... }
    public Task<bool> OnHandlingAsync(EventContext context) { ... }
    public Task OnHandledAsync(EventContext context) { ... }
}
```

**Registration:**
```csharp
services.AddDomainEvents(assembly);
services.AddSingleton<IEventMiddleware, MyMiddleware>();
```

### 2. Event Queue

In-flight non-persistent queue for events:

```csharp
// Use default in-memory queue
services.AddDomainEvents(assembly);

// Or use custom queue
services.AddSingleton<IEventQueue, MyCustomQueue>();

// Process queue
var dispatcher = serviceProvider.GetRequiredService<IEventDispatcher>();
await dispatcher.ProcessQueueAsync();
```

### 3. Custom Dispatcher

```csharp
services.AddDomainEventsWithDispatcher<MyCustomDispatcher>(assembly);
```

### 4. Standard EventInterceptor with Telemetry
The `EventInterceptor` remains standard with OpenTelemetry and logging.

### 5. Async Handler Interface (IHandler<T>)
Changed from `IHandle<T>` to `IHandler<T>` with async `HandleAsync()`.

## Breaking Changes

1. `IHandle<T>` -> `IHandler<T>`
2. `Handle()` -> `HandleAsync()` returning `Task`
3. `EventInterceptor` requires `IEventDispatcher`

## Migration Guide

```csharp
// Before
public class Handler : IHandle<Event> { void Handle(Event e) { } }

// After
public class Handler : IHandler<Event> 
{ 
    Task HandleAsync(Event e) => Task.CompletedTask; 
}
```
