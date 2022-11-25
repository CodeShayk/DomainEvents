namespace DomainEvents.Tests.Events
{
    public class CustomerCreated : IDomainEvent
    {
        public string Name { get; set; }
    }
}