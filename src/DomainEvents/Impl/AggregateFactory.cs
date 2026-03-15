using System;
using System.Linq;
using System.Reflection;
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
            var interceptor = GetInterceptor();
            
            var proxy = _proxyGenerator.CreateClassProxy<T>(constructorArguments, interceptor);
            return Task.FromResult(proxy);
        }

        /// <summary>
        /// Creates a proxied instance of the specified aggregate type using the default constructor.
        /// The proxy will intercept Raise/RaiseAsync method calls and dispatch events to handlers.
        /// </summary>
        /// <typeparam name="T">The aggregate type.</typeparam>
        /// <returns>A proxied instance of the aggregate implementing IDomainAggregate.</returns>
        public Task<T> CreateAsync<T>() where T : Aggregate
        {
            var interceptor = GetInterceptor();
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
            var interceptor = GetInterceptor();
            
            var proxy = (IDomainAggregate)_proxyGenerator.CreateClassProxy(aggregateType, constructorArguments, interceptor);
            return Task.FromResult(proxy);
        }

        /// <summary>
        /// Creates a proxied aggregate from an existing aggregate instance resolved via service locator.
        /// NOTE: All aggregate types (implementing IAggregate) must be pre-registered with the IoC container
        /// before using this method. The resolved aggregate instance will be wrapped in a proxy that intercepts
        /// Raise/RaiseAsync calls to dispatch events to registered handlers.
        /// </summary>
        /// <typeparam name="T">The aggregate type.</typeparam>
        /// <param name="aggregate">The aggregate instance resolved from the service provider.</param>
        /// <returns>A proxied instance of the aggregate implementing IDomainAggregate.</returns>
        public Task<T> CreateFromInstanceAsync<T>(T aggregate) where T : Aggregate
        {
            var interceptor = GetInterceptor();
            var proxy = _proxyGenerator.CreateClassProxyWithTarget(aggregate, interceptor);
            return Task.FromResult(proxy);
        }

        /// <summary>
        /// Creates a proxied aggregate resolved from the service provider using constructor resolution.
        /// Uses reflection to find the constructor with the most parameters, resolves those parameters
        /// from the service provider, and creates a proxy with the resolved instance.
        /// NOTE: All dependencies required by the aggregate constructor must be registered with the IoC container.
        /// </summary>
        /// <typeparam name="T">The aggregate type to resolve from the service provider.</typeparam>
        /// <returns>A proxied instance of the aggregate implementing IDomainAggregate.</returns>
        public Task<T> CreateFromServiceProviderAsync<T>() where T : Aggregate
        {
            return CreateFromServiceProviderAsync<T>(typeof(T).GetTypeInfo());
        }

        /// <summary>
        /// Creates a proxied aggregate resolved from the service provider using constructor resolution.
        /// Uses reflection to find the constructor with the most parameters, resolves those parameters
        /// from the service provider, and creates a proxy with the resolved instance.
        /// NOTE: All dependencies required by the aggregate constructor must be registered with the IoC container.
        /// </summary>
        /// <param name="aggregateType">The aggregate type to resolve from the service provider.</param>
        /// <returns>A proxied instance of the aggregate implementing IDomainAggregate.</returns>
        public Task<IDomainAggregate> CreateFromServiceProviderAsync(Type aggregateType)
        {
            return CreateFromServiceProviderAsync<IDomainAggregate>(aggregateType.GetTypeInfo());
        }

        private Task<T> CreateFromServiceProviderAsync<T>(TypeInfo aggregateTypeInfo) where T : class
        {
            var constructor = FindConstructor(aggregateTypeInfo);
            var parameters = ResolveConstructorParameters(constructor);
            var interceptor = GetInterceptor();

            var proxy = (T)_proxyGenerator.CreateClassProxy(aggregateTypeInfo.AsType(), parameters, interceptor);
            return Task.FromResult(proxy);
        }

        private ConstructorInfo FindConstructor(TypeInfo aggregateTypeInfo)
        {
            var constructors = aggregateTypeInfo.DeclaredConstructors
                .Where(c => !c.IsStatic)
                .OrderByDescending(c => c.GetParameters().Length)
                .ToList();

            if (!constructors.Any())
            {
                throw new InvalidOperationException($"No constructor found for type {aggregateTypeInfo.Name}");
            }

            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                var canResolve = parameters.All(p => _serviceProvider.GetService(p.ParameterType) != null || !p.HasDefaultValue == false);
                
                if (canResolve)
                {
                    return constructor;
                }
            }

            return constructors.First();
        }

        private object[] ResolveConstructorParameters(ConstructorInfo constructor)
        {
            var parameters = constructor.GetParameters();
            var resolved = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var service = _serviceProvider.GetService(param.ParameterType);
                
                if (service != null)
                {
                    resolved[i] = service;
                }
                else if (param.HasDefaultValue)
                {
                    resolved[i] = param.DefaultValue;
                }
                else
                {
                    resolved[i] = _serviceProvider.GetRequiredService(param.ParameterType);
                }
            }

            return resolved;
        }

        private IEventInterceptor GetInterceptor()
        {
            var interceptor = _serviceProvider.GetService<IEventInterceptor>();
            
            if (interceptor == null)
            {
                var dispatcher = _serviceProvider.GetService<IEventDispatcher>() 
                    ?? new EventDispatcher(_serviceProvider.GetRequiredService<IResolver>());
                interceptor = new EventInterceptor(dispatcher);
            }

            return interceptor;
        }
    }
}
