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

Figure below shows how consistency between aggregates is achieved by domain events. When the user initiates an order, the `Order Aggregate` sends an `OrderStarted` domain event. The OrderStarted domain event is handled by the `Buyer Aggregate` to create a Buyer object in the ordering microservice (bounded context). Please read [Domain Events](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation) for more details.

![image](https://user-images.githubusercontent.com/6259981/204060193-d2f5241e-c1d2-46ab-a16d-1c3047bc151b.png)


## Two Approaches to Use DomainEvents

### Approach 1: Using Publisher and Handler Directly

Define, publish, and subscribe to events using `IPublisher` and `IHandler`.

**1. Define an Event**
```csharp
public class CustomerCreated : IDomainEvent
{
    public string Name { get; set; }
}
```

**2. Create a Handler**
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

**3. Register Services**
```csharp
services.AddDomainEvents(typeof(CustomerCreatedHandler).Assembly);
```

**4. Publish Events**
```csharp
var publisher = serviceProvider.GetRequiredService<IPublisher>();
await publisher.RaiseAsync(new CustomerCreated { Name = "John Doe" });
```

---

### Approach 2: Using Interception (Aggregate + Factory)

Raise events automatically from domain aggregates using Castle DynamicProxy interception.

**1. Define an Event**
```csharp
public class OrderPlaced : IDomainEvent
{
    public string OrderId { get; set; }
    public decimal Amount { get; set; }
}
```

**2. Create an Aggregate (Publisher)**
```csharp
public class OrderAggregate : Aggregate
{
    public void PlaceOrder(decimal amount)
    {
        // Business logic here...
        
        var @event = new OrderPlaced
        {
            OrderId = Guid.NewGuid().ToString(),
            Amount = amount
        };
        Raise(@event);
    }
}

public class WarehouseAggregate : Aggregate, ISubscribes<OrderPlaced>
{
    public Task HandleAsync(OrderPlaced @event)
    {
        Console.WriteLine($"Order created: {@event.OrderId}");
        return Task.CompletedTask;
    }
}

```

**3. Register Services**
```csharp
services.AddDomainEvents(typeof(OrderPlacedHandler).Assembly);
```

**4. Create Aggregate and Raise Event**
```csharp
var factory = serviceProvider.GetRequiredService<IAggregateFactory>();
var order = await factory.CreateAsync<OrderAggregate>();
order.PlaceOrder(100.00m);  // Event is automatically dispatched to handlers
```

---

## Architecture Flow

### Event Processing Flow

```
┌────────────────────────────────────────────────────────────────────────────────┐
│                     PUBLISHING PHASE                                           │
│                (Aggregate.Raise() → Queue.Enqueue)                             │
└────────────────────────────────────────────────────────────────────────────────┘

  Aggregate.Raise()
        │
        ▼
  ┌───────────┐     ┌───────────┐     ┌────────────┐      ┌────────────┐
  │ Aggregate │────▶│Interceptor│────▶│ Middleware│────▶│ Dispatcher │
  │           │     │  (Proxy)  │     │(OnDispatch)│      │            │
  └───────────┘     └───────────┘     └────────────┘      └─────┬──────┘
                                                               │
                                                               ▼
                                                        ┌───────────────┐
                                                        │     Queue     │
                                                        │  (In-Memory)  │
                                                        └───────────────┘

┌────────────────────────────────────────────────────────────────────────────────┐
│                     SUBSCRIPTION PHASE                                         │
│                   (Queue → Listener → Handler)                                 │
└────────────────────────────────────────────────────────────────────────────────┘

  Queue notifies Listener
        │
        ▼
  ┌───────────┐      ┌────────────┐     ┌───────────┐      ┌───────────┐
  │ Listener  │────▶│ Middleware │────▶│ Resolver  │────▶│  Handler  │
  │           │      │(OnHandling)│     │           │      │           │
  └───────────┘      └────────────┘     └─────┬─────┘      └───────────┘
                                              │
                                              ▼
                                    ┌──────────────────┐
                                    │ IHandler<T>      │
                                    │ ISubscribes<T>   │
                                    │ (includes        │
                                    │  aggregates)     │
                                    └──────────────────┘
```

### Flow Summary

1. **PUBLISHING PHASE** - Aggregate.Raise() → Interceptor → Middleware.OnDispatching() → Dispatcher → Queue.Enqueue() → Middleware.OnDispatched()
2. **SUBSCRIPTION PHASE** - Queue notifies Listener → Middleware.OnHandling() → Resolver (finds handlers) → Handler.HandleAsync() (includes ISubscribes<T>) → Middleware.OnHandled()

**Note:** `ISubscribes` - Aggregates can implement ISubscribes<TEvent> to handle events they raise. The proxy ensures both business logic AND handler execute.

---

**Components:**
- **Aggregate** - Domain aggregate that raises events via `Raise()` or `RaiseAsync()`. Can also implement `ISubscribes<TEvent>`.
- **Interceptor** - Castle DynamicProxy that intercepts `Raise()`/`RaiseAsync()` and dispatches events.
- **Middleware** - Custom plugins: `OnDispatching`, `OnDispatched`, `OnHandling`, `OnHandled`.
- **Dispatcher** - Enqueues events to the queue.
- **Queue** - In-memory queue (fire-and-forget).
- **Listener** - Processes events from queue asynchronously.
- **Resolver** - Resolves handlers for events.
- **Handler** - Handles events: `IHandler<T>` or `ISubscribes<T>`.

---

## Event Middleware

Custom plugins that run at various points in the event pipeline:

```csharp
public class MyMiddleware : IEventMiddleware
{
    public Task<bool> OnDispatchingAsync(EventContext context)
    {
        // Runs before event is dispatched
        return Task.FromResult(true);
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

**Registration:**
```csharp
services.AddDomainEvents(assembly); // auto-registers handlers and middlewares which have parameter-less constructor. For types with parameterized constructor, you need to explicitly register as below.  
services.AddSingleton<IEventMiddleware, MyMiddleware>(); 
```

---

## AggregateFactory Methods

The `IAggregateFactory` provides multiple methods to create proxied aggregates:

| Method | Description |
|--------|-------------|
| `CreateAsync<T>()` | Creates proxy using default constructor |
| `CreateAsync<T>(params object[])` | Creates proxy with specified constructor arguments |
| `CreateAsync(Type, params object[])` | Non-generic version with constructor arguments |
| `CreateFromInstanceAsync<T>(T aggregate)` | Wraps existing aggregate instance in proxy |
| `CreateFromServiceProviderAsync<T>()` | Resolves from DI and wraps in proxy (auto-resolves constructor dependencies) |
| `CreateFromServiceProviderAsync(Type)` | Non-generic version resolving from DI |

**Example - Using CreateFromServiceProviderAsync:**
```csharp
// Register aggregate with DI (constructor dependencies auto-resolved)
services.AddTransient<OrderAggregate>();

var factory = serviceProvider.GetRequiredService<IAggregateFactory>();

// Creates proxy, resolves OrderAggregate from DI, wraps in proxy
var order = await factory.CreateFromServiceProviderAsync<OrderAggregate>();
order.PlaceOrder(100.00m);  // Events dispatched automatically
```

**Note:** When using `CreateFromServiceProviderAsync`, all constructor dependencies must be registered with the IoC container. The factory uses reflection to find the constructor with most parameters and resolves them from the service provider.

---

## Interface Summary

| Interface | Purpose |
|-----------|---------|
| `IDomainEvent` | Marker interface for domain events |
| `IHandler<TEvent>` | Async handler interface |
| `ISubscribes<TEvent>` | Aggregate handler interface (implemented by aggregates to handle their own events) |
| `IPublisher` | Interface for raising events |
| `IAggregateFactory` | Factory for creating proxied aggregates |
| `IEventMiddleware` | Plugin for event pipeline |
| `IEventQueue` | In-flight event queue |

---

## Package Information

- **Package ID**: `Dormito.DomainEvents`
- **Target Frameworks**: netstandard2.0, netstandard2.1, net8.0, net9.0, net10.0
- **License**: MIT
