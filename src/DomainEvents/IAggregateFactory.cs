using System;
using System.Threading.Tasks;

namespace DomainEvents
{
    /// <summary>
    /// Factory interface for creating proxied aggregate instances.
    /// Aggregates created through this factory will have their Raise/RaiseAsync methods
    /// intercepted to automatically dispatch events to registered handlers.
    /// </summary>
    public interface IAggregateFactory
    {
        /// <summary>
        /// Creates a proxied instance of the specified aggregate type.
        /// The proxy will intercept Raise/RaiseAsync method calls and dispatch events to handlers.
        /// </summary>
        /// <typeparam name="T">The aggregate type.</typeparam>
        /// <param name="constructorArguments">Constructor arguments for the aggregate.</param>
        /// <returns>A proxied instance of the aggregate implementing IDomainAggregate.</returns>
        Task<T> CreateAsync<T>(params object[] constructorArguments) where T : Aggregate;

        /// <summary>
        /// Creates a proxied instance of the specified aggregate type.
        /// </summary>
        /// <param name="aggregateType">The aggregate type.</param>
        /// <param name="constructorArguments">Constructor arguments for the aggregate.</param>
        /// <returns>A proxied instance of the aggregate implementing IDomainAggregate.</returns>
        Task<IDomainAggregate> CreateAsync(Type aggregateType, params object[] constructorArguments);
    }
}
