using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;

namespace DomainEvents.Impl
{
    /// <summary>
    /// Default interceptor for aggregate Raise/RaiseAsync method calls.
    /// Intercepts event raising and dispatches to registered handlers with error handling and logging.
    /// </summary>
    public class EventInterceptor : IEventInterceptor
    {
        private readonly IEventDispatcher _dispatcher;
        private readonly ILogger<EventInterceptor> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventInterceptor"/> class.
        /// </summary>
        /// <param name="dispatcher">The event dispatcher for dispatching events to handlers.</param>
        /// <param name="logger">Optional logger for diagnostic information.</param>
        public EventInterceptor(IEventDispatcher dispatcher, ILogger<EventInterceptor> logger = null)
        {
            _dispatcher = dispatcher;
            _logger = logger;
        }

        /// <summary>
        /// Intercepts the Raise/RaiseAsync method call and dispatches the event to handlers.
        /// </summary>
        /// <param name="invocation">The method invocation.</param>
        public void Intercept(IInvocation invocation)
        {
            var method = invocation.Method;
            if (!IsRaiseMethod(method))
            {
                invocation.Proceed();
                return;
            }

            var @event = invocation.Arguments[0];
            var eventType = @event.GetType();
            var methodName = method.Name;
            var isAsync = methodName == "RaiseAsync";

            _logger?.LogDebug("Intercepted {MethodName} for event type {EventType}", methodName, eventType.Name);

            var activity = DomainEventsActivitySource.Source.StartActivity(
                DomainEventsActivitySource.PublishEventActivityName,
                ActivityKind.Internal);
            
            if (activity != null)
            {
                activity.SetTag(DomainEventsTags.EventType, eventType.Name);
                activity.SetTag(DomainEventsTags.AggregateType, invocation.InvocationTarget?.GetType().Name ?? "Unknown");
            }

            try
            {
                invocation.Proceed();

                if (isAsync)
                {
                    _dispatcher.DispatchAsync(@event).GetAwaiter().GetResult();
                }
                else
                {
                    _dispatcher.Dispatch(@event);
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

        private static bool IsRaiseMethod(MethodInfo method)
        {
            return method.Name == "Raise" || method.Name == "RaiseAsync";
        }
    }
}
