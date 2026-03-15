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
        /// Creates a proxied instance of the specified aggregate type using the default constructor.
        /// The proxy will intercept Raise/RaiseAsync method calls and dispatch events to handlers.
        /// </summary>
        /// <typeparam name="T">The aggregate type.</typeparam>
        /// <returns>A proxied instance of the aggregate implementing IDomainAggregate.</returns>
        Task<T> CreateAsync<T>() where T : Aggregate;

        /// <summary>
        /// Creates a proxied instance of the specified aggregate type.
        /// </summary>
        /// <param name="aggregateType">The aggregate type.</param>
        /// <param name="constructorArguments">Constructor arguments for the aggregate.</param>
        /// <returns>A proxied instance of the aggregate implementing IDomainAggregate.</returns>
        Task<IDomainAggregate> CreateAsync(Type aggregateType, params object[] constructorArguments);

        /// <summary>
        /// Creates a proxied aggregate from an existing aggregate instance resolved via service locator.
        /// NOTE: All aggregate types (implementing IAggregate) must be pre-registered with the IoC container
        /// before using this method. The resolved aggregate instance will be wrapped in a proxy that intercepts
        /// Raise/RaiseAsync calls to dispatch events to registered handlers.
        /// </summary>
        /// <typeparam name="T">The aggregate type.</typeparam>
        /// <param name="aggregate">The aggregate instance resolved from the service provider.</param>
        /// <returns>A proxied instance of the aggregate implementing IDomainAggregate.</returns>
        Task<T> CreateFromInstanceAsync<T>(T aggregate) where T : Aggregate;

        /// <summary>
        /// Creates a proxied aggregate resolved from the service provider.
        /// NOTE: All aggregate types (implementing Aggregate) must be pre-registered with the IoC container
        /// before using this method. The resolved aggregate instance will be wrapped in a proxy that intercepts
        /// Raise/RaiseAsync calls to dispatch events to registered handlers.
        /// </summary>
        /// <typeparam name="T">The aggregate type to resolve from the service provider.</typeparam>
        /// <returns>A proxied instance of the aggregate implementing IDomainAggregate.</returns>
        Task<T> CreateFromServiceProviderAsync<T>() where T : Aggregate;

        /// <summary>
        /// Creates a proxied aggregate resolved from the service provider.
        /// NOTE: All aggregate types (implementing Aggregate) must be pre-registered with the IoC container
        /// before using this method. The resolved aggregate instance will be wrapped in a proxy that intercepts
        /// Raise/RaiseAsync calls to dispatch events to registered handlers.
        /// </summary>
        /// <param name="aggregateType">The aggregate type to resolve from the service provider.</param>
        /// <returns>A proxied instance of the aggregate implementing IDomainAggregate.</returns>
        Task<IDomainAggregate> CreateFromServiceProviderAsync(Type aggregateType);
    }
}
