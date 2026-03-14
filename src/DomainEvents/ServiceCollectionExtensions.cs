using System;
using System.Linq;
using System.Reflection;
using DomainEvents;
using DomainEvents.Impl;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DomainEvents
{
    /// <summary>
    /// Extension methods for registering domain events with dependency injection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds domain events support to the service collection.
        /// Automatically scans the specified assemblies for IHandle implementations and registers them.
        /// Uses the default EventInterceptor for event interception.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="assemblies">The assemblies to scan for event handlers.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddDomainEvents(this IServiceCollection services, params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
            {
                throw new ArgumentException("At least one assembly must be specified", nameof(assemblies));
            }

            // Register the publisher
            services.AddSingleton<IPublisher, Publisher>();

            // Register the resolver
            services.AddSingleton<IResolver>(sp =>
            {
                var handlers = sp.GetServices<IHandler>();
                return new Resolver(handlers);
            });

            // Register the default event dispatcher
            services.AddSingleton<IEventDispatcher>(sp =>
            {
                var resolver = sp.GetRequiredService<IResolver>();
                var logger = sp.GetService<ILogger<EventDispatcher>>();
                return new EventDispatcher(resolver, logger);
            });

            // Register the default event interceptor
            services.AddSingleton<IEventInterceptor>(sp =>
            {
                var dispatcher = sp.GetRequiredService<IEventDispatcher>();
                var logger = sp.GetService<ILogger<EventInterceptor>>();
                return new EventInterceptor(dispatcher, logger);
            });

            // Register the aggregate factory
            services.AddSingleton<IAggregateFactory, AggregateFactory>();

            // Scan assemblies and register all IHandler implementations with parameterless constructors
            foreach (var assembly in assemblies)
            {
                var handlerTypes = assembly.GetTypes()
                    .Where(t => !t.IsAbstract && !t.IsInterface)
                    .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandler<>)))
                    .Where(t => t.GetConstructor(Type.EmptyTypes) != null); // Only parameterless constructors

                foreach (var handlerType in handlerTypes)
                {
                    services.AddSingleton(typeof(IHandler), handlerType);
                    services.AddSingleton(handlerType);
                }
            }

            return services;
        }

        /// <summary>
        /// Adds domain events support to the service collection.
        /// Automatically scans the calling assembly for IHandle implementations and registers them.
        /// Uses the default EventInterceptor for event interception.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddDomainEvents(this IServiceCollection services)
        {
            var callingAssembly = Assembly.GetCallingAssembly();
            return services.AddDomainEvents(callingAssembly);
        }

        /// <summary>
        /// Adds domain events support with a custom event dispatcher.
        /// Use this to customize how events are dispatched to handlers.
        /// The default EventInterceptor with telemetry is still used.
        /// </summary>
        /// <typeparam name="TDispatcher">The custom dispatcher type implementing IEventDispatcher.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="assemblies">The assemblies to scan for event handlers.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddDomainEventsWithDispatcher<TDispatcher>(this IServiceCollection services, params Assembly[] assemblies)
            where TDispatcher : class, IEventDispatcher
        {
            services.AddDomainEventsCore(assemblies);
            services.AddSingleton<IEventDispatcher, TDispatcher>();
            return services;
        }

        /// <summary>
        /// Adds domain events support with a custom event dispatcher instance.
        /// Use this to customize how events are dispatched to handlers.
        /// The default EventInterceptor with telemetry is still used.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="dispatcher">The custom dispatcher instance.</param>
        /// <param name="assemblies">The assemblies to scan for event handlers.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddDomainEventsWithDispatcher(this IServiceCollection services, IEventDispatcher dispatcher, params Assembly[] assemblies)
        {
            services.AddDomainEventsCore(assemblies);
            services.AddSingleton<IEventDispatcher>(dispatcher);
            return services;
        }

        /// <summary>
        /// Adds domain events support with OpenTelemetry instrumentation.
        /// Uses the default EventInterceptor which includes OpenTelemetry activity tracking.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="assemblies">The assemblies to scan for event handlers.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddDomainEventsWithTelemetry(this IServiceCollection services, params Assembly[] assemblies)
        {
            return services.AddDomainEvents(assemblies);
        }

        /// <summary>
        /// Adds domain events support with OpenTelemetry instrumentation.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddDomainEventsWithTelemetry(this IServiceCollection services)
        {
            var callingAssembly = Assembly.GetCallingAssembly();
            return services.AddDomainEventsWithTelemetry(callingAssembly);
        }

        /// <summary>
        /// Core domain events registration without interceptor registration.
        /// </summary>
        private static IServiceCollection AddDomainEventsCore(this IServiceCollection services, Assembly[] assemblies)
        {
            // Register the publisher
            services.AddSingleton<IPublisher, Publisher>();

            // Register the resolver
            services.AddSingleton<IResolver>(sp =>
            {
                var handlers = sp.GetServices<IHandler>();
                return new Resolver(handlers);
            });

            // Register the aggregate factory
            services.AddSingleton<IAggregateFactory, AggregateFactory>();

            // Scan assemblies and register all IHandler implementations with parameterless constructors
            foreach (var assembly in assemblies)
            {
                var handlerTypes = assembly.GetTypes()
                    .Where(t => !t.IsAbstract && !t.IsInterface)
                    .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandler<>)))
                    .Where(t => t.GetConstructor(Type.EmptyTypes) != null);

                foreach (var handlerType in handlerTypes)
                {
                    services.AddSingleton(typeof(IHandler), handlerType);
                    services.AddSingleton(handlerType);
                }
            }

            return services;
        }
    }
}
