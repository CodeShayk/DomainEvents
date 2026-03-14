# Release Notes - v5.0.0

## New Features

### 1. Custom Dispatcher Support
Users can now provide their own custom dispatcher to customize how events are dispatched to handlers. This enables adding custom behavior such as logging, filtering, or transformations while keeping the standard EventInterceptor with telemetry.

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
        // Custom logic before
        Console.WriteLine($"Dispatching: {@event.GetType().Name}");
        
        // Forward to inner dispatcher
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
// Using type
services.AddDomainEventsWithDispatcher<MyCustomDispatcher>(assembly);

// Using instance
services.AddDomainEventsWithDispatcher(new MyCustomDispatcher(dispatcher), assembly);
```

### 2. IEventDispatcher Interface
Introduced `IEventDispatcher` interface to separate event dispatching logic from interception. Custom dispatchers can wrap the inner dispatcher for decorator patterns.

**Interface:**
```csharp
public interface IEventDispatcher
{
    void Dispatch(object @event);
    Task DispatchAsync(object @event);
}
```

### 3. Standard EventInterceptor with Telemetry
The `EventInterceptor` remains standard and includes:
- OpenTelemetry activity tracking
- Logging of event dispatching
- Error handling and reporting

The interceptor is automatically registered and should not be customized.

### 4. Async Handler Interface (IHandler<T>)
Changed from synchronous `IHandle<T>` to asynchronous `IHandler<T>` interface:

```csharp
// Before (v4.x)
public interface IHandle<T> : IHandle where T : IDomainEvent
{
    void Handle(T @event);
}

// After (v5.x)
public interface IHandler<T> : IHandler where T : IDomainEvent
{
    Task HandleAsync(T @event);
}
```

### 5. Service Locator Pattern in AggregateFactory
The `AggregateFactory` now uses the service locator pattern to resolve dispatchers at proxy creation time.

## Breaking Changes

### 1. IHandle -> IHandler
- `IHandle<T>` renamed to `IHandler<T>`
- `IHandle` renamed to `IHandler`
- `Handle()` method changed to `HandleAsync()` returning `Task`

### 2. EventInterceptor Constructor
- `EventInterceptor` now requires `IEventDispatcher` instead of `IResolver`
- Constructor signature: `EventInterceptor(IEventDispatcher dispatcher, ILogger logger = null)`

### 3. Service Registration
- `AddDomainEventsWithDispatcher<T>()` replaces `AddDomainEventsWithInterceptor<T>()`

## Bug Fixes

- Fixed issue where custom interceptors would not dispatch events to handlers

## Migration Guide

### Update Handlers
```csharp
// Before
public class CustomerCreatedHandler : IHandle<CustomerCreated>
{
    public void Handle(CustomerCreated @event) { }
}

// After
public class CustomerCreatedHandler : IHandler<CustomerCreated>
{
    public Task HandleAsync(CustomerCreated @event) => Task.CompletedTask;
}
```

### Update Aggregate Implementations
```csharp
// Before
public class WarehouseAggregate : Aggregate, IHandle<OrderReceived>
{
    public void Handle(OrderReceived @event) { }
}

// After
public class WarehouseAggregate : Aggregate, IHandler<OrderReceived>
{
    public Task HandleAsync(OrderReceived @event) => Task.CompletedTask;
}
```

### Adding Custom Dispatcher (instead of Interceptor)
```csharp
// Register custom dispatcher
services.AddDomainEventsWithDispatcher<LoggingDispatcher>(assembly);
```

## Dependencies
- Castle.DynamicProxy
- Microsoft.Extensions.DependencyInjection.Abstractions
- Microsoft.Extensions.Logging.Abstractions
- OpenTelemetry (optional, for telemetry support)
