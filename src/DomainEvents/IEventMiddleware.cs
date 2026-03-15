using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DomainEvents
{
    /// <summary>
    /// Middleware interface for processing events before dispatch and before handling.
    /// </summary>
    public interface IEventMiddleware
    {
        /// <summary>
        /// Called before an event is dispatched to handlers.
        /// </summary>
        /// <param name="context">The event context.</param>
        /// <returns>True to continue, false to skip dispatch.</returns>
        Task<bool> OnDispatchingAsync(EventContext context);

        /// <summary>
        /// Called after an event has been dispatched to handlers.
        /// </summary>
        /// <param name="context">The event context.</param>
        Task OnDispatchedAsync(EventContext context);

        /// <summary>
        /// Called before an event is handled by a handler.
        /// </summary>
        /// <param name="context">The event context.</param>
        /// <returns>True to continue, false to skip handling.</returns>
        Task<bool> OnHandlingAsync(EventContext context);

        /// <summary>
        /// Called after an event has been handled by a handler.
        /// </summary>
        /// <param name="context">The event context.</param>
        Task OnHandledAsync(EventContext context);
    }

    /// <summary>
    /// Context passed to middleware containing event and metadata.
    /// </summary>
    public class EventContext
    {
        public object Event { get; }
        public Type EventType { get; }
        public DateTime Timestamp { get; }
        public bool IsHandled { get; set; }
        public bool IsDispatched { get; set; }
        public Dictionary<string, object> Items { get; }

        public EventContext(object @event)
        {
            Event = @event;
            EventType = @event.GetType();
            Timestamp = DateTime.UtcNow;
            Items = new Dictionary<string, object>();
        }
    }
}
