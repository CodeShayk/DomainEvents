using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DomainEvents.Impl
{
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
