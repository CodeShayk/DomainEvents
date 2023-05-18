# <img src="https://github.com/NinjaRocks/DomainEvents/blob/master/ninja-icon-16.png" alt="ninja" style="width:30px;"/> DomainEvents v1.0.5
[![NuGet version](https://badge.fury.io/nu/Dormito.DomainEvents.svg)](https://badge.fury.io/nu/Dormito.DomainEvents) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/NinjaRocks/DomainEvents/blob/master/License.md) [![CI](https://github.com/NinjaRocks/DomainEvents/actions/workflows/CI-Build.yml/badge.svg)](https://github.com/NinjaRocks/DomainEvents/actions/workflows/CI-Build.yml) [![GitHub Release](https://img.shields.io/github/v/release/ninjarocks/DomainEvents?logo=github&sort=semver)](https://github.com/ninjarocks/DomainEvents/releases/latest)
[![CodeQL](https://github.com/NinjaRocks/DomainEvents/actions/workflows/codeql.yml/badge.svg)](https://github.com/NinjaRocks/DomainEvents/actions/workflows/codeql.yml) [![.Net Stardard](https://img.shields.io/badge/.Net%20Standard-2.1-blue)](https://dotnet.microsoft.com/en-us/download/dotnet/2.1)
## Library to help implement transactional events in domain bounded context.
Use domain events to explicitly implement side effects of changes within your domain. In other words, and using DDD terminology, use domain events to explicitly implement side effects across multiple aggregates. 
### What is a Domain Event?
> An event is something that has happened in the past. A domain event is, something that happened in the domain that you want other parts of the same domain (in-process) to be aware of. The notified parts usually react somehow to the events.
The domain events and their side effects (the actions triggered afterwards that are managed by event handlers) should occur almost immediately, usually in-process, and within the same domain.
It's important to ensure that, just like a database transaction, either all the operations related to a domain event finish successfully or none of them do.

Figure below shows how consistency between aggregates is achieved by domain events. When the user initiates an order, the `Order Aggregate` sends an `OrderStarted` domain event. The OrderStarted domain event is handled by the `Buyer Aggregate` to create a Buyer object in the ordering microservice (bounded context). Please read [Domain Events](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation) for more details.

![image](https://user-images.githubusercontent.com/6259981/204060193-d2f5241e-c1d2-46ab-a16d-1c3047bc151b.png)


### How to Define, Publish and Subscribe to an Event using DomainEvents library?

1. Define - To implement a domain event, simply derive the event class from `IDomainEvent` interface.
```
public class CustomerCreated : IDomainEvent {
        public string Name { get; set; }
}
 ```
2. Publish - To raise the domain event, Inject `IPublisher` using your favourite IoC container and call the `RaiseAsync()` method.
```
  var @event = new CustomerCreated { Name = "Ninja Sha!4h" };
  await _Publisher.RaiseAsync(@event);
```
3. Subscribe - To listen to a domain event, implement `IHandler<T>` interface where T is the event type you intend to handle.
```
public class CustomerCreatedHandler : IHandler<CustomerCreated>
{
     public Task HandleAsync(CustomerCreated @event)
     {
         Console.WriteLine($"Customer created: {@event.Name}");
         .....
     }
}
```
4. Example - IoC Container Registrations
```
public void ConfigureServices(IServiceCollection services)
{   
    // register publisher with required lifetime.
    services.AddTransient<IPublisher, Publisher>();
    
    // register all implemented event handlers.
    services.AddTransient<IHandler, CustomerCreatedHandler>();
    services.AddTransient<IHandler, OrderReceivedHandler>();
}
```
