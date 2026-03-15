# DomainEvents Library - Comprehensive Wiki

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Core Concepts](#core-concepts)
4. [Getting Started](#getting-started)
5. [Registration Methods](#registration-methods)
6. [Extension Points](#extension-points)
   - [Custom Event Dispatcher](#custom-event-dispatcher)
   - [Custom Event Queue](#custom-event-queue)
   - [Event Listener](#event-listener)
   - [Custom Event Interceptor](#custom-event-interceptor)
   - [Custom Handler Resolver](#custom-handler-resolver)
   - [Event Middleware](#event-middleware)
   - [Custom Aggregate Factory](#custom-aggregate-factory)
7. [Auto-Registration](#auto-registration)
8. [API Reference](#api-reference)
9. [Best Practices](#best-practices)
10. [Troubleshooting](#troubleshooting)

---

## Overview

DomainEvents is a library for implementing transactional domain events in domain-driven design bounded contexts. It provides a robust infrastructure for raising, dispatching, and handling domain events within your application.

### Key Features

- **Automatic Event Dispatching**: Domain aggregates automatically dispatch events when `Raise()` or `RaiseAsync()` is called
- **Middleware Pipeline**: Hook into the event lifecycle with custom middleware
- **Event Queue**: Support for in-flight event queuing
- **OpenTelemetry Integration**: Built-in telemetry support
- **Flexible Registration**: Auto-discovery of handlers and middlewares
- **Multiple Extension Points**: Customize behavior at every layer

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           Application Layer                                    │
│  ┌─────────────────┐      ┌─────────────────┐                               │
│  │  Aggregate      │      │  Publisher      │                               │
│  │  (Raise Event)  │      │  (Manual Raise) │                               │
│  └────────┬────────┘      └────────┬────────┘                               │
│           │                         │                                         │
│           ▼                         ▼                                         │
│  ┌─────────────────────────────────────────────┐                              │
│  │         EventInterceptor (Proxy)            │                              │
│  │   - Castle DynamicProxy interception        │                              │
│  │   - OpenTelemetry tracking                  │                              │
│  └─────────────────────┬───────────────────────┘                              │
│                        │                                                      │
└────────────────────────┼──────────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                      Middleware Pipeline (Dispatch)                           │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐              │
│  │ Middleware 1    │  │ Middleware 2    │  │ Middleware N    │              │
│  │ OnDispatching   │  │ OnDispatching   │  │ OnDispatching   │              │
│  │ OnDispatched    │  │ OnDispatched    │  │ OnDispatched    │              │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘              │
│           │                    │                    │                         │
│           └────────────────────┼────────────────────┘                         │
│                                │                                              │
└────────────────────────────────┼──────────────────────────────────────────────┘
                                 │
                                 ▼
┌───────────────────────────────────────────────────────────────────────────────┐
│                         Event Queue                                            │
│  ┌─────────────────────────────────────────────────────┐                      │
│  │                   InMemoryEventQueue                │                      │
│  │   - Enqueue events                                  │                      │
│  │   - Invoke subscription delegate on enqueue         │                      │
│  └─────────────────────────┬───────────────────────────┘                      │
│                            │                                                  │
│                   (delegate callback)                                         │
└────────────────────────────┼──────────────────────────────────────────────────┘
                             │
                             ▼
┌───────────────────────────────────────────────────────────────────────────────┐
│                       EventListener                                            │
│  ┌─────────────────────────────────────────────────────┐                      │
│  │              EventListener.ProcessEventAsync         │                      │
│  │   - Subscribes to queue via delegate                │                      │
│  │   - Processes events from queue                     │                      │
│  └─────────────────────────┬───────────────────────────┘                      │
│                            │                                                  │
└────────────────────────────┼──────────────────────────────────────────────────┘
                             │
                             ▼
┌───────────────────────────────────────────────────────────────────────────────┐
│                   Middleware Pipeline (Handle)                                 │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐              │
│  │ Middleware 1    │  │ Middleware 2    │  │ Middleware N    │              │
│  │ OnHandling      │  │ OnHandling      │  │ OnHandling      │              │
│  │ OnHandled       │  │ OnHandled       │  │ OnHandled       │              │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘              │
│           │                    │                    │                         │
│           └────────────────────┼────────────────────┘                         │
│                                │                                              │
└────────────────────────────────┼──────────────────────────────────────────────┘
                                 │
                                 ▼
┌───────────────────────────────────────────────────────────────────────────────┐
│                         Handler Layer                                          │
│  ┌─────────────────────────────────────────────────────┐                      │
│  │                    Resolver                         │                      │
│  │   - Resolves handlers for event type               │                      │
│  └─────────────────────────┬───────────────────────────┘                      │
│                            │                                                  │
│                            ▼                                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                        │
│  │  Handler 1   │  │  Handler 2   │  │  Handler N   │                        │
│  └──────────────┘  └──────────────┘  └──────────────┘                        │
└───────────────────────────────────────────────────────────────────────────────┘
```

### Event Flow

1. **Aggregate.Raise()** - Aggregate raises an event
2. **EventInterceptor** - Intercepts the call, proceeds with Raise, then dispatches event
3. **EventDispatcher.DispatchAsync()** - Runs dispatch middleware, enqueues event
4. **InMemoryEventQueue** - Stores event, invokes subscribed delegate immediately
5. **EventListener** - Receives callback, processes event through handle middleware
6. **Resolver** - Resolves handlers for the event type (includes `ISubscribes<T>` implementations on aggregates)
7. **Handler** - Processes the event (either standalone `IHandler<T>` or aggregate's `ISubscribes<T>.HandleAsync()`)

**Note**: The dispatcher returns immediately after enqueueing (fire-and-forget). Event processing happens asynchronously via the queue subscription delegate.

### Two-Phase Event Processing

1. **Synchronous Phase** (Aggregate.Raise → Queue.Enqueue):
   - Aggregate raises event via `Raise()` or `RaiseAsync()`
   - EventInterceptor intercepts and calls EventDispatcher
   - Dispatch middleware runs (`OnDispatchingAsync`)
   - Event is enqueued to queue
   - Dispatched middleware runs (`OnDispatchedAsync`)
   - Returns to caller (aggregate business logic completes)

2. **Asynchronous Phase** (Queue → Handler):
   - Queue notifies subscribed listener
   - Listener processes through handle middleware (`OnHandlingAsync`)
   - Resolver finds all handlers (including `ISubscribes<T>` implementations)
   - Each handler's `HandleAsync()` is called
   - Handle middleware runs (`OnHandledAsync`)

---

## Core Concepts

### Domain Events

Domain events represent something that happened in the domain that other parts need to be aware of:

```csharp
public class CustomerCreated : IDomainEvent
{
    public string CustomerId { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Event Handlers

Handlers process domain events:

```csharp
public class CustomerCreatedHandler : IHandler<CustomerCreated>
{
    public Task HandleAsync(CustomerCreated @event)
    {
        // Process the event
        Console.WriteLine($"Customer created: {@event.Name}");
        return Task.CompletedTask;
    }
}
```

### Domain Aggregates

Aggregates are domain objects that can raise events:

```csharp
public class CustomerAggregate : Aggregate
{
    public void CreateCustomer(string name)
    {
        // Business logic
        var @event = new CustomerCreated
        {
            CustomerId = Guid.NewGuid().ToString(),
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
        Raise(@event);
    }
}
```

---

## Getting Started

### 1. Install the Package

```bash
dotnet add package Dormito.DomainEvents
```

### 2. Define a Domain Event

```csharp
public class OrderPlaced : IDomainEvent
{
    public string OrderId { get; set; }
    public decimal Amount { get; set; }
}
```

### 3. Create an Event Handler

```csharp
public class OrderPlacedHandler : IHandler<OrderPlaced>
{
    public async Task HandleAsync(OrderPlaced @event)
    {
        // Send confirmation email, update inventory, etc.
        await SendConfirmationAsync(@event.OrderId);
    }
    
    private Task SendConfirmationAsync(string orderId)
    {
        // Implementation
        return Task.CompletedTask;
    }
}
```

### 4. Create an Aggregate

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
```

### 4a. Aggregate with ISubscribes (Self-Handling)

Aggregates can implement `ISubscribes<TEvent>` to handle events they raise themselves:

```csharp
public class OrderAggregate : Aggregate, ISubscribes<OrderPlaced>
{
    public Task HandleAsync(OrderPlaced @event)
    {
        // Handle the event within the same aggregate
        Console.WriteLine($"Order placed: {@event.OrderId}");
        return Task.CompletedTask;
    }

    public void PlaceOrder(decimal amount)
    {
        var @event = new OrderPlaced
        {
            OrderId = Guid.NewGuid().ToString(),
            Amount = amount
        };
        Raise(@event);
    }
}
```

**Note**: When an aggregate implements `ISubscribes<TEvent>`, the handler is called via the `Resolver` during the asynchronous event processing phase. This happens after the `Raise()` call completes (fire-and-forget pattern).

### 5. Register Services

```csharp
services.AddDomainEvents(typeof(OrderPlacedHandler).Assembly);
```

### 6. Use in Your Application

```csharp
public class OrderService
{
    private readonly IAggregateFactory _aggregateFactory;
    
    public OrderService(IAggregateFactory aggregateFactory)
    {
        _aggregateFactory = aggregateFactory;
    }
    
    public async Task PlaceOrder(decimal amount)
    {
        var order = await _aggregateFactory.CreateAsync<OrderAggregate>();
        order.PlaceOrder(amount);
        // Event is automatically dispatched to handlers
    }
}
```

---

## Registration Methods

### Basic Registration

```csharp
// Scan specific assembly
services.AddDomainEvents(typeof(OrderPlacedHandler).Assembly);

// Scan multiple assemblies
services.AddDomainEvents(
    typeof(OrderPlacedHandler).Assembly,
    typeof(CustomerCreatedHandler).Assembly
);

// Scan calling assembly
services.AddDomainEvents();
```

### With Custom Dispatcher

```csharp
services.AddDomainEventsWithDispatcher<MyCustomDispatcher>(assembly);
```

### With Custom Dispatcher Instance

```csharp
var customDispatcher = new MyCustomDispatcher();
services.AddDomainEventsWithDispatcher(customDispatcher, assembly);
```

### With Telemetry

```csharp
services.AddDomainEventsWithTelemetry(assembly);
```

### Manual Registration (Advanced)

```csharp
var services = new ServiceCollection();

// Register publisher
services.AddSingleton<IPublisher, Publisher>();

// Register resolver
services.AddSingleton<IResolver>(sp => 
    new Resolver(sp.GetServices<IHandler>()));

// Register dispatcher
services.AddSingleton<IEventDispatcher, EventDispatcher>();

// Register interceptor
services.AddSingleton<IEventInterceptor>(sp => 
    new EventInterceptor(sp.GetRequiredService<IEventDispatcher>()));

// Register aggregate factory
services.AddSingleton<IAggregateFactory, AggregateFactory>();

// Register handlers
services.AddSingleton<IHandler, OrderPlacedHandler>();
services.AddSingleton<IHandler, CustomerCreatedHandler>();

// Register middlewares
services.AddSingleton<IEventMiddleware, MyMiddleware>();
```

---

## Extension Points

### Custom Event Dispatcher

Implement `IEventDispatcher` to customize how events are dispatched. The dispatcher runs dispatch middleware and enqueues events. Event processing is handled by the EventListener via queue subscription.

```csharp
public class MyCustomDispatcher : IEventDispatcher
{
    private readonly IResolver _resolver;
    private readonly IEventQueue _queue;
    private readonly IEnumerable<IEventMiddleware> _middlewares;
    private readonly ILogger<MyCustomDispatcher> _logger;

    public MyCustomDispatcher(
        IResolver resolver,
        IEventQueue queue = null,
        IEnumerable<IEventMiddleware> middlewares = null,
        ILogger<MyCustomDispatcher> logger = null)
    {
        _resolver = resolver;
        _queue = queue ?? new InMemoryEventQueue();
        _middlewares = middlewares ?? Enumerable.Empty<IEventMiddleware>();
        _logger = logger;
    }

    public void Dispatch(object @event)
    {
        // Custom synchronous dispatch logic
        var context = new EventContext(@event);
        DispatchWithMiddlewareAsync(context).GetAwaiter().GetResult();
    }

    public async Task DispatchAsync(object @event)
    {
        var context = new EventContext(@event);
        await DispatchWithMiddlewareAsync(context);
    }

    private async Task DispatchWithMiddlewareAsync(EventContext context)
    {
        // Run dispatch middleware (before)
        foreach (var middleware in _middlewares)
        {
            if (!await middleware.OnDispatchingAsync(context))
            {
                _logger?.LogDebug("Middleware skipped dispatching");
                return;
            }
        }

        // Enqueue event - EventListener will process via subscription
        await _queue.EnqueueAsync(context);

        context.IsDispatched = true;
        
        // Run dispatch middleware (after)
        foreach (var middleware in _middlewares)
        {
            await middleware.OnDispatchedAsync(context);
        }
    }

    public IEventQueue Queue => _queue;
}

---

### Custom Event Queue
            {
                await middleware.OnHandledAsync(context);
            }
        }
    }

    public IEventQueue Queue => _queue;

    public async Task ProcessQueueAsync()
    {
        while (_queue.Count > 0)
        {
            var context = await _queue.DequeueAsync();
            if (context != null)
            {
                await ProcessEventAsync(context);
            }
        }
    }
}
```

**Registration:**

```csharp
services.AddDomainEventsWithDispatcher<MyCustomDispatcher>(assembly);

// Or with instance
var dispatcher = new MyCustomDispatcher(resolver);
services.AddDomainEventsWithDispatcher(dispatcher, assembly);
```

---

### Custom Event Queue

Implement `IEventQueue` to create a custom queue (e.g., persistent queue, distributed queue):

```csharp
public class MyCustomQueue : IEventQueue
{
    private readonly Queue<EventContext> _queue = new Queue<EventContext>();
    private EventDequeuedHandler _handler;
    private readonly object _lock = new object();
    
    public Task EnqueueAsync(EventContext context)
    {
        lock (_lock)
        {
            _queue.Enqueue(context);
        }

        // Immediately invoke the subscribed handler (fire-and-forget)
        _handler?.Invoke(context);

        return Task.CompletedTask;
    }

#if NET8_0_OR_GREATER
    public Task<EventContext?> DequeueAsync()
#else
    public Task<EventContext> DequeueAsync()
#endif
    {
        lock (_lock)
        {
            if (_queue.Count > 0)
            {
#if NET8_0_OR_GREATER
                return Task.FromResult<EventContext?>(_queue.Dequeue());
#else
                return Task.FromResult(_queue.Dequeue());
#endif
            }
        }
#if NET8_0_OR_GREATER
        return Task.FromResult<EventContext?>(null);
#else
        throw new InvalidOperationException("Queue is empty");
#endif
    }

    public IReadOnlyList<EventContext> PeekAll()
    {
        lock (_lock)
        {
            return _queue.ToArray();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _queue.Clear();
        }
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _queue.Count;
            }
        }
    }

    public void Subscribe(EventDequeuedHandler handler)
    {
        _handler = handler;
    }
}
```

**Key Points:**
- The `Subscribe` method registers a delegate that gets called when events are enqueued
- The delegate is invoked immediately in `EnqueueAsync` (synchronous callback)
- This enables fire-and-forget event processing

**Registration:**

```csharp
services.AddDomainEvents(assembly);
services.AddSingleton<IEventQueue, MyCustomQueue>();
```

---

### Custom Event Interceptor

Implement `IEventInterceptor` to customize how aggregate methods are intercepted:

```csharp
public class MyCustomInterceptor : IEventInterceptor
{
    private readonly IEventDispatcher _dispatcher;
    private readonly ILogger<MyCustomInterceptor> _logger;

    public MyCustomInterceptor(
        IEventDispatcher dispatcher,
        ILogger<MyCustomInterceptor> logger = null)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public void Intercept(IInvocation invocation)
    {
        var method = invocation.Method;
        
        // Check if it's a Raise or RaiseAsync method
        if (!IsRaiseMethod(method))
        {
            invocation.Proceed();
            return;
        }

        var @event = invocation.Arguments[0];
        var eventType = @event.GetType();
        var methodName = method.Name;
        var isAsync = methodName == "RaiseAsync";

        _logger?.LogDebug("Intercepted {MethodName} for {EventType}", methodName, eventType.Name);

        try
        {
            // Proceed with the original method (executes Raise body)
            invocation.Proceed();

            // Dispatch the event
            if (isAsync)
            {
                _dispatcher.DispatchAsync(@event).GetAwaiter().GetResult();
            }
            else
            {
                _dispatcher.Dispatch(@event);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error dispatching event {EventType}", eventType.Name);
            throw;
        }
    }

    private static bool IsRaiseMethod(MethodInfo method)
    {
        return method.Name == "Raise" || method.Name == "RaiseAsync";
    }
}
```

**Registration:**

```csharp
services.AddDomainEvents(assembly);
services.AddSingleton<IEventInterceptor, MyCustomInterceptor>();
```

---

### Custom Handler Resolver

Implement `IResolver` to customize how handlers are resolved:

```csharp
public class MyCustomResolver : IResolver
{
    private readonly IEnumerable<IHandler> _handlers;
    private readonly Dictionary<Type, List<IHandler>> _handlerCache;

    public MyCustomResolver(IEnumerable<IHandler> handlers)
    {
        _handlers = handlers;
        _handlerCache = new Dictionary<Type, List<IHandler>>();
        
        // Build handler cache
        foreach (var handler in _handlers)
        {
            var handlerType = handler.GetType();
            var interfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandler<>));
            
            foreach (var iface in interfaces)
            {
                var eventType = iface.GetGenericArguments()[0];
                if (!_handlerCache.ContainsKey(eventType))
                {
                    _handlerCache[eventType] = new List<IHandler>();
                }
                _handlerCache[eventType].Add(handler);
            }
        }
    }

    public Task<IEnumerable<IHandler<T>>> ResolveAsync<T>() where T : IDomainEvent
    {
        var eventType = typeof(T);
        
        if (_handlerCache.TryGetValue(eventType, out var handlers))
        {
            var typedHandlers = handlers.Cast<IHandler<T>>();
            return Task.FromResult<IEnumerable<IHandler<T>>>(typedHandlers);
        }
        
        return Task.FromResult<IEnumerable<IHandler<T>>>(Enumerable.Empty<IHandler<T>>());
    }
}
```

**Registration:**

```csharp
services.AddSingleton<IResolver, MyCustomResolver>();
```

---

### Event Middleware

Middleware allows you to hook into the event pipeline at various points:

```csharp
public class MyMiddleware : IEventMiddleware
{
    private readonly ILogger<MyMiddleware> _logger;

    public MyMiddleware(ILogger<MyMiddleware> logger)
    {
        _logger = logger;
    }

    // Called before event is dispatched to handlers
    public Task<bool> OnDispatchingAsync(EventContext context)
    {
        _logger.LogInformation("About to dispatch event: {EventType}", context.EventType.Name);
        
        // Return false to skip dispatching
        // Return true to continue
        return Task.FromResult(true);
    }

    // Called after event has been dispatched to all handlers
    public Task OnDispatchedAsync(EventContext context)
    {
        _logger.LogInformation("Event dispatched: {EventType}", context.EventType.Name);
        return Task.CompletedTask;
    }

    // Called before each handler processes the event
    public Task<bool> OnHandlingAsync(EventContext context)
    {
        _logger.LogDebug("About to handle event: {EventType}", context.EventType.Name);
        return Task.FromResult(true);
    }

    // Called after each handler processes the event
    public Task OnHandledAsync(EventContext context)
    {
        _logger.LogDebug("Event handled: {EventType}", context.EventType.Name);
        return Task.CompletedTask;
    }
}
```

**Using the Base Class:**

```csharp
public class LoggingMiddleware : EventMiddlewareBase
{
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public override Task<bool> OnDispatchingAsync(EventContext context)
    {
        _logger.LogInformation("Event dispatching: {EventType}", context.EventType.Name);
        return base.OnDispatchingAsync(context);
    }

    public override Task OnDispatchedAsync(EventContext context)
    {
        _logger.LogInformation("Event dispatched: {EventType}", context.EventType.Name);
        return base.OnDispatchedAsync(context);
    }
}
```

**Registration:**

```csharp
// Manual registration
services.AddDomainEvents(assembly);
services.AddSingleton<IEventMiddleware, MyMiddleware>();

// Or auto-registration (requires parameterless constructor)
services.AddDomainEvents(assembly);
// Middlewares with parameterless constructors are auto-registered
```

**Middleware with Dependencies:**

If your middleware requires dependencies, register it manually (not auto-registered):

```csharp
services.AddSingleton<IEventMiddleware>(sp => 
    new MyMiddleware(sp.GetRequiredService<ILogger<MyMiddleware>>()));
```

---

### Event Listener

Implement `IEventListener` to customize how events are processed from the queue:

```csharp
public class MyEventListener : IEventListener
{
    private readonly IEventQueue _queue;
    private readonly IResolver _resolver;
    private readonly IEnumerable<IEventMiddleware> _middlewares;
    private readonly ILogger<MyEventListener> _logger;

    public MyEventListener(
        IEventQueue queue,
        IResolver resolver,
        IEnumerable<IEventMiddleware> middlewares = null,
        ILogger<MyEventListener> logger = null)
    {
        _queue = queue;
        _resolver = resolver;
        _middlewares = middlewares ?? Enumerable.Empty<IEventMiddleware>();
        _logger = logger;

        // Subscribe to queue - this is called when events are enqueued
        _queue.Subscribe(OnEventEnqueued);
    }

    private Task OnEventEnqueued(EventContext context)
    {
        return ProcessEventAsync(context);
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Event listener started");
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _logger?.LogInformation("Event listener stopped");
    }

    public async Task ProcessEventAsync(EventContext context)
    {
        // Process event through middleware and handlers
        var handlers = await _resolver.ResolveAsync(context.EventType);
        
        foreach (var handler in handlers)
        {
            // Run handling middleware (before)
            foreach (var middleware in _middlewares)
            {
                if (!await middleware.OnHandlingAsync(context))
                    continue;
            }

            // Invoke handler
            var handlerInterfaceType = typeof(IHandler<>).MakeGenericType(context.EventType);
            var handleMethod = handlerInterfaceType.GetMethod("HandleAsync");
            handleMethod?.Invoke(handler, new[] { context.Event });
            
            context.IsHandled = true;

            // Run handling middleware (after)
            foreach (var middleware in _middlewares)
            {
                await middleware.OnHandledAsync(context);
            }
        }
    }
}
```

**Key Points:**
- The listener subscribes to the queue via `_queue.Subscribe(OnEventEnqueued)`
- When an event is enqueued, the delegate is invoked immediately
- The listener handles the processing pipeline: middleware -> resolver -> handler
- The EventListener is automatically registered when using `AddDomainEvents`

**Registration:**

```csharp
services.AddDomainEvents(assembly);
// EventListener is auto-registered and subscribes automatically
```

---

### Custom Aggregate Factory

Implement `IAggregateFactory` to customize how aggregates are created:

```csharp
public class MyAggregateFactory : IAggregateFactory
{
    private readonly ProxyGenerator _proxyGenerator;
    private readonly IServiceProvider _serviceProvider;

    public MyAggregateFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _proxyGenerator = new ProxyGenerator();
    }

    public Task<T> CreateAsync<T>(params object[] constructorArguments) where T : Aggregate
    {
        var interceptor = _serviceProvider.GetService<IEventInterceptor>();
        
        if (interceptor == null)
        {
            throw new InvalidOperationException("IEventInterceptor not registered");
        }
        
        var proxy = _proxyGenerator.CreateClassProxy<T>(interceptor);
        return Task.FromResult(proxy);
    }

    public Task<IDomainAggregate> CreateAsync(Type aggregateType, params object[] constructorArguments)
    {
        var interceptor = _serviceProvider.GetService<IEventInterceptor>();
        
        if (interceptor == null)
        {
            throw new InvalidOperationException("IEventInterceptor not registered");
        }
        
        var proxy = (IDomainAggregate)_proxyGenerator.CreateClassProxy(aggregateType, interceptor);
        return Task.FromResult(proxy);
    }
}
```

**Registration:**

```csharp
services.AddSingleton<IAggregateFactory, MyAggregateFactory>();
```

---

## Auto-Registration

The library automatically discovers and registers components from specified assemblies. Only types with **parameterless constructors** are auto-registered. Types with constructor parameters must be registered explicitly.

### What Gets Auto-Registered

| Component | Requirement | Behavior |
|-----------|-------------|----------|
| Event Handlers (`IHandler<T>`) | Parameterless constructor | Singleton |
| Event Middleware (`IEventMiddleware`) | Parameterless constructor | Singleton |

### Auto-Registration Behavior

1. **Handlers**: All types implementing `IHandler<T>` with parameterless constructors are registered
2. **Middlewares**: All types implementing `IEventMiddleware` with parameterless constructors are registered
3. **Manual Override**: If you manually register a service before calling `AddDomainEvents`, the auto-registration skips that specific type

### Types with Parameters - Must Register Explicitly

If a handler or middleware has constructor parameters, it will **not** be auto-registered. You must register it explicitly:

**Handler with dependencies (must register manually):**
```csharp
public class OrderHandler : IHandler<OrderPlaced>
{
    private readonly IOrderService _orderService;
    
    // Has constructor parameter - won't be auto-registered
    public OrderHandler(IOrderService orderService)
    {
        _orderService = orderService;
    }
    
    public Task HandleAsync(OrderPlaced @event)
    {
        return _orderService.ProcessAsync(@event);
    }
}

// Must register explicitly:
services.AddSingleton<IHandler, OrderHandler>();
services.AddSingleton<IOrderService, OrderService>();
```

**Middleware with dependencies (must register manually):**
```csharp
public class AuditMiddleware : IEventMiddleware
{
    private readonly IAuditService _auditService;
    
    // Has constructor parameter - won't be auto-registered
    public AuditMiddleware(IAuditService auditService)
    {
        _auditService = auditService;
    }
    
    public Task<bool> OnDispatchingAsync(EventContext context)
    {
        return _auditService.LogAsync(context.Event);
    }
    
    // ... other interface implementations
}

// Must register explicitly:
services.AddSingleton<IEventMiddleware, AuditMiddleware>();
services.AddSingleton<IAuditService, AuditService>();
```

### Example: Auto-Registration

```csharp
// This will auto-register all handlers and middlewares with parameterless constructors
services.AddDomainEvents(typeof(MyHandler).Assembly);
```

### Example: Preventing Auto-Registration

To prevent auto-registration, add a constructor with parameters:

```csharp
// Won't be auto-registered (has constructor parameter)
public class MyMiddleware : IEventMiddleware
{
    public MyMiddleware(string name) { } // Requires parameter
    
    // ... interface implementations
}

// Will be auto-registered (parameterless constructor)
public class AnotherMiddleware : IEventMiddleware
{
    public AnotherMiddleware() { } // Parameterless
    
    // ... interface implementations
}
```

---

## API Reference

### Interfaces

| Interface | Description |
|-----------|-------------|
| `IDomainEvent` | Marker interface for domain events |
| `IHandler<TEvent>` | Async handler interface for specific event type |
| `ISubscribes<TEvent>` | Aggregate handler interface - implemented by aggregates to handle their own events |
| `IHandler` | Marker interface for handlers |
| `IPublisher` | Interface for manually raising events |
| `IResolver` | Interface for resolving handlers |
| `IEventDispatcher` | Interface for dispatching events |
| `IEventInterceptor` | Interceptor for aggregate Raise/RaiseAsync methods |
| `IEventMiddleware` | Middleware for event pipeline |
| `IEventQueue` | Queue for in-flight events with subscription support |
| `IEventListener` | Listener for processing queued events via subscription |
| `IAggregateFactory` | Factory for creating proxied aggregates |

### Delegates

| Delegate | Description |
|----------|-------------|
| `EventDequeuedHandler` | Delegate for processing dequeued events (signature: `Task Handler(EventContext context)`) |

### Classes

| Class | Description |
|-------|-------------|
| `Aggregate` | Base class for domain aggregates |
| `EventContext` | Context passed to middleware |
| `Publisher` | Default implementation of IPublisher |
| `Resolver` | Default implementation of IResolver |
| `EventDispatcher` | Default implementation of IEventDispatcher |
| `EventListener` | Default implementation of IEventListener - subscribes to queue and processes events |
| `EventInterceptor` | Default interceptor with telemetry |
| `AggregateFactory` | Default factory for proxied aggregates |
| `InMemoryEventQueue` | Default in-memory queue with subscription support |
| `EventMiddlewareBase` | Base class for middleware |
| `LoggingMiddleware` | Built-in logging middleware |

### ServiceCollectionExtensions

| Method | Description |
|--------|-------------|
| `AddDomainEvents(assemblies)` | Register with default configuration |
| `AddDomainEvents()` | Register for calling assembly |
| `AddDomainEventsWithDispatcher<TDispatcher>(assemblies)` | Register with custom dispatcher type |
| `AddDomainEventsWithDispatcher(dispatcher, assemblies)` | Register with custom dispatcher instance |
| `AddDomainEventsWithTelemetry(assemblies)` | Register with OpenTelemetry support |

### IAggregateFactory Methods

The `IAggregateFactory` provides multiple methods to create proxied aggregates:

| Method | Description |
|--------|-------------|
| `CreateAsync<T>()` | Creates proxy using default constructor |
| `CreateAsync<T>(params object[])` | Creates proxy with specified constructor arguments |
| `CreateAsync(Type, params object[])` | Non-generic version with constructor arguments |
| `CreateFromInstanceAsync<T>(T aggregate)` | Wraps existing aggregate instance in proxy |
| `CreateFromServiceProviderAsync<T>()` | Resolves from DI and wraps in proxy (auto-resolves constructor deps) |
| `CreateFromServiceProviderAsync(Type)` | Non-generic version resolving from DI |

**Example - Using CreateFromServiceProviderAsync:**

```csharp
// Register aggregate with DI (constructor dependencies auto-resolved)
services.AddTransient<OrderAggregate>();
services.AddTransient<IOrderService, OrderService>();

var factory = serviceProvider.GetRequiredService<IAggregateFactory>();

// Creates proxy, resolves OrderAggregate from DI, wraps in proxy
var order = await factory.CreateFromServiceProviderAsync<OrderAggregate>();
order.PlaceOrder(100.00m);  // Events dispatched automatically
```

**Note:** When using `CreateFromServiceProviderAsync`, all constructor dependencies must be registered with the IoC container. The factory uses reflection to find the constructor with most parameters and resolves them from the service provider.

---

## Best Practices

### 1. Keep Handlers Focused

Each handler should do one thing:

```csharp
// Good
public class OrderConfirmationHandler : IHandler<OrderPlaced>
{
    public Task HandleAsync(OrderPlaced @event) =>
        SendEmailAsync(@event.CustomerId, "Order confirmed");
}

public class InventoryHandler : IHandler<OrderPlaced>
{
    public Task HandleAsync(OrderPlaced @event) =>
        ReserveInventoryAsync(@event.Items);
}

// Avoid - handlers doing too much
public class OrderPlacedHandler : IHandler<OrderPlaced>
{
    public Task HandleAsync(OrderPlaced @event)
    {
        // Don't do email, inventory, analytics, etc. all here
    }
}
```

### 2. Use Middleware for Cross-Cutting Concerns

```csharp
public class AuditMiddleware : EventMiddlewareBase
{
    private readonly IAuditService _auditService;

    public AuditMiddleware(IAuditService auditService)
    {
        _auditService = auditService;
    }

    public override async Task OnDispatchedAsync(EventContext context)
    {
        await _auditService.LogAsync(context.Event, context.EventType.Name);
    }
}
```

### 3. Handle Errors in Middleware

```csharp
public class ErrorHandlingMiddleware : EventMiddlewareBase
{
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public override async Task OnDispatchedAsync(EventContext context)
    {
        if (context.IsDispatched)
        {
            _logger.LogInformation("Successfully handled {EventType}", context.EventType.Name);
        }
    }
}
```

### 4. Use EventContext.Items for State Sharing

```csharp
public class TrackingMiddleware : EventMiddlewareBase
{
    public override Task<bool> OnDispatchingAsync(EventContext context)
    {
        context.Items["CorrelationId"] = Guid.NewGuid();
        return base.OnDispatchingAsync(context);
    }
}

public class AnotherMiddleware : EventMiddlewareBase
{
    public override Task OnHandledAsync(EventContext context)
    {
        var correlationId = context.Items["CorrelationId"];
        // Use correlation ID for logging/tracing
        return base.OnHandledAsync(context);
    }
}
```

### 5. Don't Block in Middleware

```csharp
// Bad - blocks the thread
public Task<bool> OnDispatchingAsync(EventContext context)
{
    Thread.Sleep(1000); // Don't do this
    return Task.FromResult(true);
}

// Good - async/await
public async Task<bool> OnDispatchingAsync(EventContext context)
{
    await Task.Delay(1000); // Non-blocking
    return true;
}
```

---

## Troubleshooting

### Events Not Being Dispatched

1. **Check if aggregate is proxied**:
   ```csharp
   // Use IAggregateFactory to create aggregates
   var order = await aggregateFactory.CreateAsync<OrderAggregate>();
   order.PlaceOrder(100); // This will dispatch events
   
   // Direct instantiation won't dispatch
   var order2 = new OrderAggregate();
   order2.PlaceOrder(100); // Events won't be dispatched
   ```

2. **Check handler registration**:
   ```csharp
   var handlers = serviceProvider.GetServices<IHandler>();
   // Should contain your handlers
   ```

3. **Check middleware returning false**:
   ```csharp
   // If any middleware returns false in OnDispatchingAsync, events won't be dispatched
   public Task<bool> OnDispatchingAsync(EventContext context)
   {
       return Task.FromResult(false); // This blocks dispatch
   }
   ```

### Middleware Not Called

1. **Check registration**:
   ```csharp
   // Make sure middleware is registered
   services.AddSingleton<IEventMiddleware, MyMiddleware>();
   ```

2. **Check constructor**:
   ```csharp
   // Middleware must have parameterless constructor OR be manually registered
   public class MyMiddleware : IEventMiddleware
   {
       // This requires manual registration
       public MyMiddleware(ILogger<MyMiddleware> logger) { }
   }
   ```

### Handlers Not Found

1. **Check assembly scanning**:
   ```csharp
   // Make sure the assembly contains handlers
   services.AddDomainEvents(typeof(MyHandler).Assembly);
   ```

2. **Check handler interface**:
   ```csharp
   // Must implement IHandler<T> where T : IDomainEvent
   public class MyHandler : IHandler<MyEvent> // Correct
   {
       public Task HandleAsync(MyEvent e) => Task.CompletedTask;
   }
   ```

### Queue Not Processing

1. **Call ProcessQueueAsync**:
   ```csharp
   var dispatcher = serviceProvider.GetRequiredService<IEventDispatcher>();
   await dispatcher.ProcessQueueAsync();
   ```

2. **Check queue is registered**:
   ```csharp
   services.AddSingleton<IEventQueue, MyQueue>();
   ```

---

## Migration Guide

### From v4 to v5

v5 introduces breaking changes:

1. **Event Dispatcher now receives middlewares**:
   ```csharp
   // v4
   services.AddSingleton<IEventDispatcher>(sp => 
       new EventDispatcher(sp.GetRequiredService<IResolver>()));
   
   // v5
   services.AddSingleton<IEventDispatcher>(sp => 
       new EventDispatcher(
           sp.GetRequiredService<IResolver>(),
           sp.GetService<IEventQueue>(),
           sp.GetServices<IEventMiddleware>(),
           sp.GetService<ILogger<EventDispatcher>>()));
   ```

2. **Use AddDomainEvents for full setup**:
   ```csharp
   // Recommended
   services.AddDomainEvents(assembly);
   
   // Manual registration is still supported for advanced scenarios
   ```

### Adding to Existing Project

1. Install the package:
   ```bash
   dotnet add package Dormito.DomainEvents
   ```

2. Update registration:
   ```csharp
   services.AddDomainEvents(typeof(YourHandler).Assembly);
   ```

3. Use IAggregateFactory:
   ```csharp
   public class OrderService
   {
       private readonly IAggregateFactory _factory;
       
       public OrderService(IAggregateFactory factory)
       {
           _factory = factory;
       }
       
       public async Task PlaceOrder()
       {
           var order = await _factory.CreateAsync<OrderAggregate>();
           order.Place(100);
       }
   }
   ```

---

## License

MIT License - see [LICENSE](LICENSE) for details.
