using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DomainEvents.Impl
{
    /// <summary>
    /// Default implementation of IEventListener that subscribes to the queue.
    /// Processes events immediately when they are enqueued.
    /// </summary>
    public class EventListener : IEventListener
    {
        private readonly IEventQueue _queue;
        private readonly IResolver _resolver;
        private readonly IEnumerable<IEventMiddleware> _middlewares;
        private readonly ILogger<EventListener> _logger;
        private CancellationTokenSource _cts;

        public EventListener(
            IEventQueue queue,
            IResolver resolver,
            IEnumerable<IEventMiddleware> middlewares = null,
            ILogger<EventListener> logger = null)
        {
            _queue = queue;
            _resolver = resolver;
            _middlewares = middlewares ?? Enumerable.Empty<IEventMiddleware>();
            _logger = logger;

            _queue.Subscribe(OnEventEnqueued);
            _logger?.LogInformation("EventListener created and subscribed to queue");
        }

        private Task OnEventEnqueued(EventContext context)
        {
            return ProcessEventAsync(context);
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _logger?.LogInformation("Event listener started");
            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            _cts?.Cancel();
            _logger?.LogInformation("Event listener stopped");
        }

        public async Task ProcessEventAsync(EventContext context)
        {
            var eventType = context.EventType;
            
            if (!(_resolver is Resolver resolver))
            {
                _logger?.LogWarning("Resolver is not of type Resolver, cannot dispatch event");
                return;
            }

            var handlers = await resolver.ResolveAsync(eventType);
            var handlerList = handlers.ToList();

            _logger?.LogDebug("Found {HandlerCount} handlers for event {EventType}", handlerList.Count, eventType.Name);

            var exceptions = new List<Exception>();

            foreach (var handler in handlerList)
            {
                var handlerType = handler.GetType();
                
                var activity = DomainEventsActivitySource.Source.StartActivity(
                    DomainEventsActivitySource.HandleEventActivityName,
                    ActivityKind.Internal);

                if (activity != null)
                {
                    activity.SetTag(DomainEventsTags.EventType, eventType.Name);
                    activity.SetTag(DomainEventsTags.HandlerType, handlerType.Name);
                }

                try
                {
                    foreach (var middleware in _middlewares)
                    {
                        if (!await middleware.OnHandlingAsync(context))
                        {
                            _logger?.LogDebug("Middleware skipped handling for {EventType}", eventType.Name);
                            continue;
                        }
                    }

                    var handlerInterfaceType = typeof(IHandler<>).MakeGenericType(eventType);
                    var handleMethod = handlerInterfaceType.GetMethod("HandleAsync");
                    handleMethod?.Invoke(handler, new[] { context.Event });
                    
                    context.IsHandled = true;

                    foreach (var middleware in _middlewares)
                    {
                        await middleware.OnHandledAsync(context);
                    }

                    if (activity != null)
                    {
                        activity.SetStatus(ActivityStatusCode.Ok);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in handler {HandlerType} for event {EventType}", 
                        handlerType.Name, eventType.Name);
                    if (activity != null)
                    {
                        activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                        activity.SetTag(DomainEventsTags.ErrorType, ex.GetType().FullName);
                        activity.SetTag(DomainEventsTags.ErrorMessage, ex.Message);
                    }
                    exceptions.Add(ex);
                }
                finally
                {
                    activity?.Dispose();
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException($"Errors occurred while dispatching event {eventType.Name}", exceptions);
            }
        }
    }
}
