using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DomainEvents
{
    /// <summary>
    /// Base class for event middleware with no-op implementations.
    /// </summary>
    public abstract class EventMiddlewareBase : IEventMiddleware
    {
        public virtual Task<bool> OnDispatchingAsync(EventContext context)
        {
            return Task.FromResult(true);
        }

        public virtual Task OnDispatchedAsync(EventContext context)
        {
            return Task.CompletedTask;
        }

        public virtual Task<bool> OnHandlingAsync(EventContext context)
        {
            return Task.FromResult(true);
        }

        public virtual Task OnHandledAsync(EventContext context)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Logging middleware for events.
    /// </summary>
    public class LoggingMiddleware : EventMiddlewareBase
    {
        private readonly ILogger<LoggingMiddleware> _logger;

        public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
        {
            _logger = logger;
        }

        public override Task<bool> OnDispatchingAsync(EventContext context)
        {
            _logger.LogInformation("Event dispatching: {EventType}", context.EventType.Name);
            return base.OnDispatchingAsync(context);
        }

        public override Task OnDispatchedAsync(EventContext context)
        {
            _logger.LogInformation("Event dispatched: {EventType}", context.EventType.Name);
            return base.OnDispatchedAsync(context);
        }

        public override Task<bool> OnHandlingAsync(EventContext context)
        {
            _logger.LogDebug("Event handling: {EventType}", context.EventType.Name);
            return base.OnHandlingAsync(context);
        }

        public override Task OnHandledAsync(EventContext context)
        {
            _logger.LogDebug("Event handled: {EventType}", context.EventType.Name);
            return base.OnHandledAsync(context);
        }
    }
}
