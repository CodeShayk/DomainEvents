# DomainEvents [![NuGet version](https://badge.fury.io/nu/Dormito.DomainEvents.svg)](https://badge.fury.io/nu/Dormito.DomainEvents) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/NinjaRocks/Ninja.DomainEvents/blob/master/LICENSE) [![.NET](https://github.com/NinjaRocks/Ninja.DomainEvents/actions/workflows/build.yml/badge.svg)](https://github.com/NinjaRocks/Ninja.DomainEvents/actions/workflows/build.yml) [![Lint Code Base](https://github.com/NinjaRocks/Ninja.DomainEvents/actions/workflows/linter.yml/badge.svg)](https://github.com/NinjaRocks/Ninja.DomainEvents/actions/workflows/linter.yml)
## Library to offer transactional events in domain model.
Use domain events to explicitly implement side effects of changes within your domain. In other words, and using DDD terminology, use domain events to explicitly implement side effects across multiple aggregates. 
### What is a domain event?
> An event is something that has happened in the past. A domain event is, something that happened in the domain that you want other parts of the same domain (in-process) to be aware of. The notified parts usually react somehow to the events.
The domain events and their side effects (the actions triggered afterwards that are managed by event handlers) should occur almost immediately, usually in-process, and within the same domain.
It's important to ensure that, just like a database transaction, either all the operations related to a domain event finish successfully or none of them do.

Figure below shows how consistency between aggregates is achieved by domain events. When the user initiates an order, the `Order Aggregate` sends an `OrderStarted` domain event. The OrderStarted domain event is handled by the `Buyer Aggregate` to create a Buyer object in the ordering microservice (bounded context).

![Example](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/media/domain-events-design-implementation/domain-model-ordering-microservice.png)

Please read [Domain Events](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation) for more details.
 

### DomainEvents
How to `Define`, `Publish` and `Subscribe` to a Domain event using DomainEvents library.

> To implement a domain event, simply derive the event class from `IDomainEvent` interface.
```
public class CustomerCreated : IDomainEvent {
        public string Name { get; set; }
}
 ```
> To Publish the domain event, Inject `IPublisher` using your favourite IoC container.
```
  var @event = new CustomerCreated { Name = "Ninja Sha!4h" };
  await _Publisher.RaiseAsync(@event);
```
> To subscribe to a domain event, implement IHandle<T> interface where T is the event type.
```
public class CustomerCreatedHandler : IHandle<CustomerCreated>
{
     public Task HandleAsync(CustomerCreated @event)
     {
         Console.WriteLine($"Customer created: {@event.Name}");
         .....
     }
}
```
> IoC Container Registrations
TBC
