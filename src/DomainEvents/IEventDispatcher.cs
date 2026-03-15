using System;
using System.Threading.Tasks;

namespace DomainEvents
{
    /// <summary>
    /// Interface for dispatching domain events to registered handlers.
    /// </summary>
    public interface IEventDispatcher
    {
        /// <summary>
        /// Dispatches an event to all registered handlers synchronously.
        /// </summary>
        /// <param name="event">The event to dispatch.</param>
        void Dispatch(object @event);

        /// <summary>
        /// Dispatches an event to all registered handlers asynchronously.
        /// </summary>
        /// <param name="event">The event to dispatch.</param>
        Task DispatchAsync(object @event);
    }
}