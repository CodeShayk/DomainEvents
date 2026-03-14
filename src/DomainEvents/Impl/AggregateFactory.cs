using System;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;

namespace DomainEvents.Impl
{
    /// <summary>
    /// Factory for creating proxied aggregate instances.
    /// Aggregates created through this factory will have their Raise/RaiseAsync methods
    /// intercepted to automatically dispatch events to registered handlers.
    /// </summary>
    public class AggregateFactory : IAggregateFactory
    {
        private readonly ProxyGenerator _proxyGenerator = new ProxyGenerator();
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateFactory"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for resolving interceptors.</param>
        public AggregateFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Creates a proxied instance of the specified aggregate type.
        /// The proxy will intercept Raise/RaiseAsync method calls and dispatch events to handlers.
        /// </summary>
        /// <typeparam name="T">The aggregate type.</typeparam>
        /// <param name="constructorArguments">Constructor arguments for the aggregate.</param>
        /// <returns>A proxied instance of the aggregate implementing IDomainAggregate.</returns>
        public Task<T> CreateAsync<T>(params object[] constructorArguments) where T : Aggregate
        {
            var interceptor = _serviceProvider.GetService<IEventInterceptor>();
            
            if (interceptor == null)
            {
                var dispatcher = _serviceProvider.GetService<IEventDispatcher>() 
                    ?? new EventDispatcher(_serviceProvider.GetRequiredService<IResolver>());
                interceptor = new EventInterceptor(dispatcher);
            }
            
            var proxy = _proxyGenerator.CreateClassProxy<T>(interceptor);
            return Task.FromResult(proxy);
        }

        /// <summary>
        /// Creates a proxied instance of the specified aggregate type.
        /// </summary>
        /// <param name="aggregateType">The aggregate type.</param>
        /// <param name="constructorArguments">Constructor arguments for the aggregate.</param>
        /// <returns>A proxied instance of the aggregate implementing IDomainAggregate.</returns>
        public Task<IDomainAggregate> CreateAsync(Type aggregateType, params object[] constructorArguments)
        {
            var interceptor = _serviceProvider.GetService<IEventInterceptor>();
            
            if (interceptor == null)
            {
                var dispatcher = _serviceProvider.GetService<IEventDispatcher>() 
                    ?? new EventDispatcher(_serviceProvider.GetRequiredService<IResolver>());
                interceptor = new EventInterceptor(dispatcher);
            }
            
            var proxy = (IDomainAggregate)_proxyGenerator.CreateClassProxy(aggregateType, interceptor);
            return Task.FromResult(proxy);
        }
    }
}
