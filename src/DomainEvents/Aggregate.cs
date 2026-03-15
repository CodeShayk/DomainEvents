using System.Threading.Tasks;

namespace DomainEvents
{
    /// <summary>
    /// Abstract base class for domain aggregates that can raise and handle domain events.
    /// Aggregates derived from this class can raise events which will be intercepted
    /// and dispatched to registered handlers via Castle DynamicProxy.
    /// </summary>
    public abstract class Aggregate : IDomainAggregate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Aggregate"/> class.
        /// </summary>
        protected Aggregate()
        {
        }

        /// <summary>
        /// Raises a domain event synchronously. The event will be intercepted and dispatched
        /// to all registered handlers for the event type when using proxied aggregates.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event to raise.</typeparam>
        /// <param name="event">The event instance to raise.</param>
        protected virtual void Raise<TEvent>(TEvent @event) where TEvent : IDomainEvent
        {
            // Empty body - intercepted by EventInterceptor when using proxied aggregates
        }

        /// <summary>
        /// Raises a domain event asynchronously. The event will be intercepted and dispatched
        /// to all registered handlers for the event type when using proxied aggregates.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event to raise.</typeparam>
        /// <param name="event">The event instance to raise.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public virtual Task RaiseAsync<TEvent>(TEvent @event) where TEvent : IDomainEvent
        {
            // Empty body - intercepted by EventInterceptor when using proxied aggregates
            return Task.CompletedTask;
        }
    }
}
