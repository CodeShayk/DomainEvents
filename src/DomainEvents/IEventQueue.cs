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

    /// <summary>
    /// Default in-memory implementation of IEventQueue with subscription support.
    /// </summary>
    public class InMemoryEventQueue : IEventQueue
    {
        private readonly Queue<EventContext> _queue = new Queue<EventContext>();
        private EventDequeuedHandler _handler;
        private readonly object _lock = new object();

        public Task EnqueueAsync(EventContext context)
        {
            lock (_lock)
            {
                _queue.Enqueue(context);
            }

            _handler?.Invoke(context);

            return Task.CompletedTask;
        }

#if NET8_0_OR_GREATER
        public Task<EventContext?> DequeueAsync()
#else
        public Task<EventContext> DequeueAsync()
#endif
        {
            lock (_lock)
            {
                if (_queue.Count > 0)
                {
#if NET8_0_OR_GREATER
                    return Task.FromResult<EventContext?>(_queue.Dequeue());
#else
                    return Task.FromResult(_queue.Dequeue());
#endif
                }
            }
#if NET8_0_OR_GREATER
            return Task.FromResult<EventContext?>(null);
#else
            throw new InvalidOperationException("Queue is empty");
#endif
        }

        public IReadOnlyList<EventContext> PeekAll()
        {
            lock (_lock)
            {
                return _queue.ToArray();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _queue.Clear();
            }
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _queue.Count;
                }
            }
        }

        public void Subscribe(EventDequeuedHandler handler)
        {
            _handler = handler;
        }
    }
}
