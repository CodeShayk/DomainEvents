using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DomainEvents
{
    /// <summary>
    /// Delegate type for processing dequeued events.
    /// </summary>
    /// <param name="context">The event context to process.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public delegate Task EventDequeuedHandler(EventContext context);

    /// <summary>
    /// In-flight non-persistent queue for events with subscription support.
    /// </summary>
    public interface IEventQueue
    {
        /// <summary>
        /// Enqueues an event for async processing.
        /// </summary>
        /// <param name="context">The event context.</param>
        Task EnqueueAsync(EventContext context);

        /// <summary>
        /// Dequeues an event for processing.
        /// </summary>
        /// <returns>The event context or null if queue is empty.</returns>
#if NET8_0_OR_GREATER
        Task<EventContext?> DequeueAsync();
#else
        Task<EventContext> DequeueAsync();
#endif

        /// <summary>
        /// Gets all queued events.
        /// </summary>
        IReadOnlyList<EventContext> PeekAll();

        /// <summary>
        /// Clears all queued events.
        /// </summary>
        void Clear();

        /// <summary>
        /// Gets the count of queued events.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Registers a handler that will be called when an event is enqueued.
        /// Only one handler can be registered. Calling this again replaces the previous handler.
        /// </summary>
        /// <param name="handler">The handler to call when an event is enqueued.</param>
        void Subscribe(EventDequeuedHandler handler);
    }
}
