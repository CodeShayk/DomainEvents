namespace DomainEvents.Tests.Events
{
    public class OrderReceived : IDomainEvent
    {
        public string OrderNo { get; set; }
    }
}