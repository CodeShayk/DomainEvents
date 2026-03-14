# <img src="https://github.com/CodeShayk/DomainEvents/blob/master/images/pub-sub-icon.png" alt="events" style="width:100px;"/> DomainEvents v4.0.1
[![NuGet version](https://badge.fury.io/nu/Dormito.DomainEvents.svg)](https://badge.fury.io/nu/Dormito.DomainEvents)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/CodeShayk/DomainEvents/blob/master/License.md)
[![Build](https://github.com/CodeShayk/DomainEvents/actions/workflows/master-build.yml/badge.svg)](https://github.com/CodeShayk/DomainEvents/actions/workflows/master-build.yml)
[![CodeQL](https://github.com/CodeShayk/DomainEvents/actions/workflows/master-codeQL.yml/badge.svg)](https://github.com/CodeShayk/DomainEvents/actions/workflows/master-codeQL.yml)
[![GitHub Release](https://img.shields.io/github/v/release/CodeShayk/DomainEvents?logo=github&sort=semver)](https://github.com/CodeShayk/DomainEvents/releases/latest)
[![.Net 9.0](https://img.shields.io/badge/.Net-9.0-blue)](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
[![.Net Standard 2.0](https://img.shields.io/badge/.NetStandard-2.0-green)](https://github.com/dotnet/standard/blob/v2.0.0/docs/versions/netstandard2.0.md)
[![.Net Framework 4.6.4](https://img.shields.io/badge/.Net-4.6.4-blue)](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net46)
## Library to help implement transactional events in domain bounded context.

Use domain events to explicitly implement side effects of changes within your domain. In other words, and using DDD terminology, use domain events to explicitly implement side effects across multiple aggregates.

### What is a Domain Event?

> An event is something that has happened in the past. A domain event is, something that happened in the domain that you want other parts of the same domain (in-process) to be aware of. The notified parts usually react somehow to the events.

The domain events and their side effects (the actions triggered afterwards that are managed by event handlers) should occur almost immediately, usually in-process, and within the same domain.

It's important to ensure that, just like a database transaction, either all the operations related to a domain event finish successfully or none of them do.

---

## Architecture Flow

```
Aggregate -> Interceptor -> Middleware -> Dispatcher -> Queue <- Listener -> Middleware -> Resolver -> Handler
```

### Components:

1. **Aggregate** - Domain aggregate that raises events
2. **Interceptor** - Castle DynamicProxy interceptor (standard, with telemetry)
3. **Middleware** - Custom plugins that run before/after dispatch and handling
4. **Dispatcher** - Dispatches events to handlers (customizable)
5. **Queue** - In-flight non-persistent queue for events (optional)
6. **Listener** - Listens to queue and triggers handling
7. **Resolver** - Resolves handlers for events
8. **Handler** - Handles the events

---

## v5.x - New Features

### 1. Event Middleware

Custom plugins that run at various points in the event pipeline:

```csharp
public class MyMiddleware : IEventMiddleware
{
    public Task<bool> OnDispatchingAsync(EventContext context)
    {
        // Runs before event is dispatched
        Console.WriteLine($"Before dispatch: {context.EventType.Name}");
        return Task.FromResult(true); // Return false to skip
    }

    public Task OnDispatchedAsync(EventContext context)
    {
        // Runs after event is dispatched
        return Task.CompletedTask;
    }

    public Task<bool> OnHandlingAsync(EventContext context)
    {
        // Runs before each handler processes the event
        return Task.FromResult(true);
    }

    public Task OnHandledAsync(EventContext context)
    {
        // Runs after each handler processes the event
        return Task.CompletedTask;
    }
}
```

Register middleware:

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
```

Process queue:

```csharp
var dispatcher = serviceProvider.GetRequiredService<IEventDispatcher>();
await dispatcher.ProcessQueueAsync();
```

### 3. Custom Dispatcher

Customize how events are dispatched:

```csharp
public class MyCustomDispatcher : IEventDispatcher
{
    private readonly IEventDispatcher _innerDispatcher;
    
    public MyCustomDispatcher(IEventDispatcher innerDispatcher)
    {
        _innerDispatcher = innerDispatcher;
    }
    
    public void Dispatch(object @event)
    {
        // Custom logic
        _innerDispatcher.Dispatch(@event);
    }
    
    public Task DispatchAsync(object @event)
    {
        Dispatch(@event);
        return Task.CompletedTask;
    }
}
```

**Registration:**
```csharp
services.AddDomainEventsWithDispatcher<MyCustomDispatcher>(assembly);
```

### 4. Standard EventInterceptor with Telemetry

The `EventInterceptor` provides standard interception with:
- OpenTelemetry activity tracking
- Logging
- Error handling

### 5. Async Handler Interface (IHandler<T>)

```csharp
public class CustomerCreatedHandler : IHandler<CustomerCreated>
{
    public Task HandleAsync(CustomerCreated @event)
    {
        Console.WriteLine($"Customer created: {@event.Name}");
        return Task.CompletedTask;
    }
}
```

---

## Usage Patterns

### Pattern 1: Basic Usage

```csharp
services.AddDomainEvents(typeof(CustomerCreatedHandler).Assembly);
```

### Pattern 2: With Custom Middleware

```csharp
services.AddDomainEvents(assembly);
services.AddSingleton<IEventMiddleware, LoggingMiddleware>();
services.AddSingleton<IEventMiddleware, MetricsMiddleware>();
```

### Pattern 3: With Custom Queue

```csharp
services.AddDomainEvents(assembly);
services.AddSingleton<IEventQueue, MyCustomQueue>();

// Process events from queue
var dispatcher = serviceProvider.GetRequiredService<IEventDispatcher>();
await dispatcher.ProcessQueueAsync();
```

### Pattern 4: With Custom Dispatcher

```csharp
services.AddDomainEventsWithDispatcher<MyCustomDispatcher>(assembly);
```

---

## Interface Summary

| Interface | Purpose |
|-----------|---------|
| `IDomainEvent` | Marker interface for domain events |
| `IHandler<TEvent>` | Async handler interface |
| `IHandler` | Marker interface for handlers |
| `IPublisher` | Interface for raising events |
| `IResolver` | Interface for resolving handlers |
| `IEventInterceptor` | Interceptor for Raise/RaiseAsync |
| `IEventDispatcher` | Dispatches events to handlers |
| `IEventMiddleware` | Plugin for event pipeline |
| `IEventQueue` | In-flight event queue |
| `IEventListener` | Queue listener |

## Implementation Classes

| Class | Purpose |
|-------|---------|
| `Aggregate` | Base class for domain aggregates |
| `Publisher` | IPublisher implementation |
| `Resolver` | IResolver implementation |
| `AggregateFactory` | Creates proxied aggregates |
| `EventInterceptor` | Default interceptor with telemetry |
| `EventDispatcher` | Default dispatcher |
| `InMemoryEventQueue` | Default in-memory queue |
| `EventMiddlewareBase` | Base class for middleware |
| `LoggingMiddleware` | Built-in logging middleware |

## Package Information

- **Package ID**: `Dormito.DomainEvents`
- **Target Frameworks**: netstandard2.0, netstandard2.1, net8.0, net9.0, net10.0
- **License**: MIT
