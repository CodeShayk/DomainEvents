# Release Notes - v5.0.0

## New Architecture
```
Aggregate → Interceptor → Middleware → Dispatcher → Queue ← Listener → Resolver → Handler
```

## New Features

### 1. ISubscribes<TEvent>
Aggregates can handle their own events:
```csharp
public class OrderAggregate : Aggregate, ISubscribes<OrderPlaced>
{
    public Task HandleAsync(OrderPlaced @event) => ...;
    public void PlaceOrder(decimal amount) => Raise(new OrderPlaced());
}
```

### 2. AggregateFactory
Multiple methods to create proxied aggregates:
```csharp
// Default constructor
var order = await factory.CreateAsync<OrderAggregate>();

// With constructor arguments
var order = await factory.CreateAsync<OrderAggregate>(logger);

// From service provider (auto-resolves deps)
var order = await factory.CreateFromServiceProviderAsync<OrderAggregate>();

// Wrap existing instance
var order = await factory.CreateFromInstanceAsync(existingOrder);
```

### 3. Event Middleware (IEventMiddleware)
Pipeline hooks: `OnDispatchingAsync`, `OnDispatchedAsync`, `OnHandlingAsync`, `OnHandledAsync`

### 4. Event Queue (IEventQueue)
In-flight non-persistent queue with subscription support

### 5. Event Listener (IEventListener)
Processes queued events asynchronously

## Breaking Changes
- `IHandle<T>` → `IHandler<T>`
- `Handle()` → `HandleAsync()` returning `Task`

## Migration
```csharp
// Before
public class Handler : IHandle<Event> { void Handle(Event e) { } }

// After
public class Handler : IHandler<Event> { Task HandleAsync(Event e) => Task.CompletedTask; }
```
