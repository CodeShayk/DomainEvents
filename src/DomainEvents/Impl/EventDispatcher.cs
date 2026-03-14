using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DomainEvents.Impl;
using Microsoft.Extensions.Logging;

namespace DomainEvents
{
    /// <summary>
    /// Default implementation of IEventDispatcher that dispatches events to registered handlers.
    /// Events are enqueued and the dispatcher completes immediately. Listeners process via queue subscription.
    /// </summary>
    public class EventDispatcher : IEventDispatcher
    {
        private readonly IResolver _resolver;
        private readonly IEventQueue _queue;
        private readonly IEnumerable<IEventMiddleware> _middlewares;
        private readonly ILogger<EventDispatcher> _logger;

        public EventDispatcher(
            IResolver resolver,
            IEventQueue queue = null,
            IEnumerable<IEventMiddleware> middlewares = null,
            ILogger<EventDispatcher> logger = null)
        {
            _resolver = resolver;
            _queue = queue ?? new InMemoryEventQueue();
            _middlewares = middlewares ?? Enumerable.Empty<IEventMiddleware>();
            _logger = logger;
        }

        public IEventQueue Queue => _queue;

        public void Dispatch(object @event)
        {
            if (@event == null) return;

            var context = new EventContext(@event);
            
            DispatchWithMiddlewareAsync(context).GetAwaiter().GetResult();
        }

        public async Task DispatchAsync(object @event)
        {
            if (@event == null) return;

            var context = new EventContext(@event);
            await DispatchWithMiddlewareAsync(context);
        }

        private async Task DispatchWithMiddlewareAsync(EventContext context)
        {
            var eventType = context.EventType;
            _logger?.LogDebug("Dispatching event {EventType}", eventType.Name);

            var activity = DomainEventsActivitySource.Source.StartActivity(
                DomainEventsActivitySource.PublishEventActivityName,
                ActivityKind.Internal);

            if (activity != null)
            {
                activity.SetTag(DomainEventsTags.EventType, eventType.Name);
            }

            try
            {
                foreach (var middleware in _middlewares)
                {
                    if (!await middleware.OnDispatchingAsync(context))
                    {
                        _logger?.LogDebug("Middleware {Middleware} skipped dispatching for {EventType}", 
                            middleware.GetType().Name, eventType.Name);
                        return;
                    }
                }

                await _queue.EnqueueAsync(context);
                _logger?.LogDebug("Event {EventType} enqueued", eventType.Name);

                context.IsDispatched = true;
                
                foreach (var middleware in _middlewares)
                {
                    await middleware.OnDispatchedAsync(context);
                }

                _logger?.LogDebug("Successfully dispatched event {EventType}", eventType.Name);
                
                if (activity != null)
                {
                    activity.SetStatus(ActivityStatusCode.Ok);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error dispatching event {EventType}", eventType.Name);
                if (activity != null)
                {
                    activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                    activity.SetTag(DomainEventsTags.ErrorType, ex.GetType().FullName);
                    activity.SetTag(DomainEventsTags.ErrorMessage, ex.Message);
                }
                throw;
            }
            finally
            {
                activity?.Dispose();
            }
        }
    }
}
