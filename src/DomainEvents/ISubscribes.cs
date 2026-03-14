using System.Threading.Tasks;

namespace DomainEvents
{
    /// <summary>
    /// Interface for domain event subscriptions implemented by aggregates.
    /// Aggregates can implement this interface to explicitly subscribe to and handle domain events.
    /// </summary>
    /// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
    public interface ISubscribes<TEvent> : IHandler<TEvent> where TEvent : IDomainEvent
    {
    }
}
