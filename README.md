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

```
Aggregate -> Interceptor -> Middleware -> Dispatcher -> Queue <- Listener -> Middleware -> Resolver -> Handler
```

**Components:**
- **Aggregate** - Domain aggregate that raises events via `Raise()` or `RaiseAsync()`
- **Interceptor** - Castle DynamicProxy interceptor (automatically dispatches events)
- **Middleware** - Custom plugins that run before/after dispatch and handling
- **Dispatcher** - Dispatches events (enqueues to queue)
- **Queue** - In-flight non-persistent queue (fire-and-forget)
- **Listener** - Processes events from queue via subscription delegate
- **Resolver** - Resolves handlers for events
- **Handler** - Handles the events

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
services.AddDomainEvents(assembly);
services.AddSingleton<IEventMiddleware, MyMiddleware>();
```

---

## Interface Summary

| Interface | Purpose |
|-----------|---------|
| `IDomainEvent` | Marker interface for domain events |
| `IHandler<TEvent>` | Async handler interface |
| `IPublisher` | Interface for raising events |
| `IAggregateFactory` | Factory for creating proxied aggregates |
| `IEventMiddleware` | Plugin for event pipeline |
| `IEventQueue` | In-flight event queue |

---

## Package Information

- **Package ID**: `Dormito.DomainEvents`
- **Target Frameworks**: netstandard2.0, netstandard2.1, net8.0, net9.0, net10.0
- **License**: MIT
