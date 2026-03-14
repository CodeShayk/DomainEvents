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

Figure below shows how consistency between aggregates is achieved by domain events. When the user initiates an order, the `Order Aggregate` sends an `OrderStarted` domain event. The OrderStarted domain event is handled by the `Buyer Aggregate` to create a Buyer object in the ordering microservice (bounded context). Please read [Domain Events](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation) for more details.

![image](https://user-images.githubusercontent.com/6259981/204060193-d2f5241e-c1d2-46ab-a16d-1c3047bc151b.png)

---

## v5.x - New Features

### 1. Async Handler Interface (IHandler<T>)

The library now uses async handlers with `IHandler<T>` interface:

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

### 2. Custom Dispatcher Support

You can now provide your own custom dispatcher to customize how events are dispatched to handlers. The standard EventInterceptor with telemetry remains unchanged.

```csharp
public class MyCustomDispatcher : IEventDispatcher
{
    public void Dispatch(object @event)
    {
        // Custom dispatch logic
        Console.WriteLine($"Dispatching event: {@event.GetType().Name}");
        
        // Resolve handlers and dispatch
        // ...
    }

    public Task DispatchAsync(object @event)
    {
        Dispatch(@event);
        return Task.CompletedTask;
    }
}
```

Register with DI:

```csharp
// Using type
services.AddDomainEventsWithDispatcher<MyCustomDispatcher>(assembly);

// Using instance
services.AddDomainEventsWithDispatcher(new MyCustomDispatcher(), assembly);
```

### 3. Standard EventInterceptor with Telemetry

The `EventInterceptor` provides standard interception with built-in telemetry:

- OpenTelemetry activity tracking
- Logging of event dispatching
- Error handling and reporting

The interceptor is automatically registered and should not be customized.

### 4. Aggregate Base Class

The library provides an `Aggregate` base class that can raise and handle domain events:

```csharp
public class CustomerAggregate : Aggregate
{
    public void RegisterCustomer(string name)
    {
        var @event = new CustomerCreated { Name = name };
        Raise(@event);  // Automatically intercepted and dispatched
    }
}

public class WarehouseAggregate : Aggregate, IHandler<OrderReceived>
{
    public Task HandleAsync(OrderReceived @event)
    {
        Console.WriteLine($"Warehouse received order: {@event.OrderNo}");
        return Task.CompletedTask;
    }

    public void ProcessOrder(string orderNo)
    {
        Raise(new OrderReceived { OrderNo = orderNo });
    }
}
```

### 5. Microsoft.Extensions.DependencyInjection Integration

Automatically scan assemblies and register all event handlers:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Auto-scan assemblies for IHandler implementations
    services.AddDomainEvents(typeof(MyHandler).Assembly);
}
```

### 6. Castle DynamicProxy Interception

Use the `IAggregateFactory` to create proxied aggregates with automatic event interception:

```csharp
// Register in DI
services.AddDomainEvents(assembly);
services.AddSingleton<IAggregateFactory, AggregateFactory>();

// Usage
var factory = serviceProvider.GetRequiredService<IAggregateFactory>();
var customer = await factory.CreateAsync<CustomerAggregate>();
customer.RegisterCustomer("John Doe");  // Automatically intercepted
```

---

## Usage Patterns

### Pattern 1: Traditional Publisher/Handler

1. **Define Event** - Derive from `IDomainEvent`:

```csharp
public class CustomerCreated : IDomainEvent
{
    public string Name { get; set; }
}
```

2. **Publish** - Inject `IPublisher` and call `RaiseAsync()`:

```csharp
var @event = new CustomerCreated { Name = "John" };
await _publisher.RaiseAsync(@event);
```

3. **Subscribe** - Implement `IHandler<T>` interface:

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

4. **Auto-Register with DI**:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddDomainEvents(typeof(CustomerCreatedHandler).Assembly);
}
```

### Pattern 2: Aggregate-Based with Auto-Registration

1. **Define Event**:

```csharp
public class OrderCreated : IDomainEvent
{
    public string OrderId { get; set; }
}
```

2. **Create Aggregate**:

```csharp
public class OrderAggregate : Aggregate
{
    public void CreateOrder(string customerId)
    {
        Raise(new OrderCreated { OrderId = Guid.NewGuid().ToString() });
    }
}
```

3. **Create Handler (Aggregate that handles events)**:

```csharp
public class InventoryAggregate : Aggregate, IHandler<OrderCreated>
{
    public Task HandleAsync(OrderCreated @event)
    {
        Console.WriteLine($"Reserving inventory for order: {@event.OrderId}");
        return Task.CompletedTask;
    }
}
```

4. **Auto-Register with DI**:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddDomainEvents(typeof(OrderCreated).Assembly);
}
```

### Pattern 3: Custom Dispatcher

To add custom dispatch logic (e.g., logging, filtering, transformations):

```csharp
public class LoggingDispatcher : IEventDispatcher
{
    private readonly IEventDispatcher _innerDispatcher;
    private readonly ILogger<LoggingDispatcher> _logger;

    public LoggingDispatcher(IEventDispatcher innerDispatcher, ILogger<LoggingDispatcher> logger)
    {
        _innerDispatcher = innerDispatcher;
        _logger = logger;
    }

    public void Dispatch(object @event)
    {
        _logger.LogInformation("Dispatching event: {EventType}", @event.GetType().Name);
        _innerDispatcher.Dispatch(@event);
    }

    public Task DispatchAsync(object @event)
    {
        Dispatch(@event);
        return Task.CompletedTask;
    }
}

// Register
services.AddDomainEventsWithDispatcher<LoggingDispatcher>(assembly);
```

---

## Interface Summary

| Interface | Purpose |
|-----------|---------|
| `IDomainEvent` | Marker interface for domain events |
| `IHandler<TEvent>` | Async handler interface for specific event types |
| `IHandler` | Marker interface for handlers |
| `IPublisher` | Interface for raising/publishing domain events |
| `IResolver` | Interface for resolving handlers for a given event type |
| `IEventInterceptor` | Interface for intercepting Raise/RaiseAsync calls (standard, with telemetry) |
| `IEventDispatcher` | Interface for dispatching events to handlers (customizable) |

## Implementation Classes

| Class | Purpose |
|-------|---------|
| `Aggregate` | Base class for domain aggregates with Raise() method |
| `Publisher` | Concrete implementation of `IPublisher` |
| `Resolver` | Concrete implementation of `IResolver` |
| `AggregateFactory` | Factory for creating proxied aggregate instances |
| `EventInterceptor` | Default Castle DynamicProxy interceptor with telemetry |
| `EventDispatcher` | Default implementation of `IEventDispatcher` |

## Package Information

- **Package ID**: `Dormito.DomainEvents`
- **Target Frameworks**: netstandard2.0, netstandard2.1, net8.0, net9.0, net10.0
- **License**: MIT
- **Repository**: https://github.com/CodeShayk/DomainEvents
