using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DomainEvents.Impl;
using Microsoft.Extensions.Logging;

namespace DomainEvents
{
    /// <summary>
    /// Default implementation of IEventDispatcher that dispatches events to registered handlers.
    /// </summary>
    public class EventDispatcher : IEventDispatcher
    {
        private readonly IResolver _resolver;
        private readonly ILogger<EventDispatcher> _logger;

        public EventDispatcher(IResolver resolver, ILogger<EventDispatcher> logger = null)
        {
            _resolver = resolver;
            _logger = logger;
        }

        public void Dispatch(object @event)
        {
            if (@event == null) return;

            var eventType = @event.GetType();
            _logger?.LogDebug("Dispatching event {EventType}", eventType.Name);

            if (!(_resolver is Resolver resolver))
            {
                _logger?.LogWarning("Resolver is not of type Resolver, cannot dispatch event");
                return;
            }

            var handlers = resolver.ResolveAsync(eventType).GetAwaiter().GetResult();
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
                    var handlerInterfaceType = typeof(IHandler<>).MakeGenericType(eventType);
                    var handleMethod = handlerInterfaceType.GetMethod("HandleAsync");
                    handleMethod?.Invoke(handler, new[] { @event });
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

        public async Task DispatchAsync(object @event)
        {
            if (@event == null) return;

            var eventType = @event.GetType();
            _logger?.LogDebug("Dispatching event async {EventType}", eventType.Name);

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
                    var handlerInterfaceType = typeof(IHandler<>).MakeGenericType(eventType);
                    var handleMethod = handlerInterfaceType.GetMethod("HandleAsync");
                    handleMethod?.Invoke(handler, new[] { @event });
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