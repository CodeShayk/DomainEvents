using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DomainEvents.Impl
{
    /// <summary>
    /// Default Resolver to return all handlers implemented for event type T.
    /// </summary>
    public class Resolver : IResolver
    {
        protected readonly IEnumerable<IHandler> _handlers;

        public Resolver(IEnumerable<IHandler> handlers)
        {
            _handlers = handlers;
        }

        public Task<IEnumerable<IHandler<T>>> ResolveAsync<T>() where T : IDomainEvent
        {
            var handlers = _handlers.OfType<IHandler<T>>();
            return Task.FromResult(handlers);
        }

        /// <summary>
        /// Resolves handlers for a given event type at runtime.
        /// </summary>
        /// <param name="eventType">The event type.</param>
        /// <returns>All handlers for the specified event type.</returns>
        public virtual Task<IEnumerable<IHandler>> ResolveAsync(Type eventType)
        {
            var handlers = _handlers.Where(h => h.GetType().GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandler<>)
                && i.GetGenericArguments()[0] == eventType));
            return Task.FromResult(handlers);
        }
    }
}