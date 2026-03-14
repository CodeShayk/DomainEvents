using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DomainEvents.Impl;
using DomainEvents.Tests.Aggregates;
using DomainEvents.Tests.Events;
using DomainEvents.Tests.Handlers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace DomainEvents.Tests.Run
{
    /// <summary>
    /// Tests for middleware functionality.
    /// </summary>
    public class MiddlewareTests
    {
        private IServiceProvider _serviceProvider;

        [SetUp]
        public void Setup()
        {
            TestMiddleware.BeforeDispatchCount = 0;
            TestMiddleware.AfterDispatchCount = 0;
            TestMiddleware.BeforeHandleCount = 0;
            TestMiddleware.AfterHandleCount = 0;
            SimpleCustomerCreatedHandler.HandleCount = 0;
        }

        [TearDown]
        public void TearDown()
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        [Test]
        public void AddDomainEvents_ShouldRegisterMiddleware()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IEventMiddleware, TestMiddleware>();
            services.AddDomainEvents(typeof(SimpleCustomerCreatedHandler).Assembly);
            _serviceProvider = services.BuildServiceProvider();

            // Act
            var middlewares = _serviceProvider.GetServices<IEventMiddleware>().ToList();

            // Assert - TestMiddleware manually registered, SkippingMiddleware has constructor param so not auto-registered
            Assert.That(middlewares.Count, Is.EqualTo(1));
            Assert.That(middlewares.Any(m => m is TestMiddleware), Is.True);
        }

        [Test]
        public void AddDomainEvents_ShouldAutoRegisterMiddleware()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDomainEvents(typeof(SimpleCustomerCreatedHandler).Assembly);
            _serviceProvider = services.BuildServiceProvider();

            // Act
            var middlewares = _serviceProvider.GetServices<IEventMiddleware>().ToList();

            // Assert - Only TestMiddleware is auto-registered (SkippingMiddleware has constructor param)
            Assert.That(middlewares.Count, Is.EqualTo(1));
            Assert.That(middlewares.Any(m => m is TestMiddleware), Is.True);
        }

        [Test]
        public async Task Middleware_ShouldCallOnDispatching()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IEventMiddleware, TestMiddleware>();
            services.AddDomainEvents(typeof(SimpleCustomerCreatedHandler).Assembly);
            _serviceProvider = services.BuildServiceProvider();
            var factory = _serviceProvider.GetRequiredService<IAggregateFactory>();

            var customer = await factory.CreateAsync<CustomerAggregate>();

            // Act
            customer.RegisterCustomer("Test");

            // Assert
            Assert.That(TestMiddleware.BeforeDispatchCount, Is.EqualTo(1));
        }

        [Test]
        public async Task Middleware_ShouldCallOnDispatched()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IEventQueue, InMemoryEventQueue>();
            services.AddSingleton<IEventMiddleware, TestMiddleware>();
            services.AddSingleton<IHandler, SimpleCustomerCreatedHandler>();
            services.AddSingleton<IResolver>(sp => new Resolver(sp.GetServices<IHandler>()));
            services.AddSingleton<IEventDispatcher, EventDispatcher>();
            services.AddSingleton<IEventListener>(sp => 
                new EventListener(
                    sp.GetRequiredService<IEventQueue>(),
                    sp.GetRequiredService<IResolver>(),
                    sp.GetServices<IEventMiddleware>()));
            services.AddSingleton<IEventInterceptor>(sp => new EventInterceptor(sp.GetRequiredService<IEventDispatcher>()));
            services.AddSingleton<IAggregateFactory, AggregateFactory>();
            _serviceProvider = services.BuildServiceProvider();
            _serviceProvider.GetService<IEventListener>(); // Resolve to trigger subscription
            var factory = _serviceProvider.GetRequiredService<IAggregateFactory>();

            var customer = await factory.CreateAsync<CustomerAggregate>();

            // Act
            customer.RegisterCustomer("Test");

            // Assert
            Assert.That(TestMiddleware.AfterDispatchCount, Is.EqualTo(1));
        }

        [Test]
        public async Task Middleware_ShouldCallOnHandling()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IEventQueue, InMemoryEventQueue>();
            services.AddSingleton<IEventMiddleware, TestMiddleware>();
            services.AddSingleton<IHandler, SimpleCustomerCreatedHandler>();
            services.AddSingleton<IResolver>(sp => new Resolver(sp.GetServices<IHandler>()));
            services.AddSingleton<IEventDispatcher, EventDispatcher>();
            services.AddSingleton<IEventListener>(sp => 
                new EventListener(
                    sp.GetRequiredService<IEventQueue>(),
                    sp.GetRequiredService<IResolver>(),
                    sp.GetServices<IEventMiddleware>()));
            services.AddSingleton<IEventInterceptor>(sp => new EventInterceptor(sp.GetRequiredService<IEventDispatcher>()));
            services.AddSingleton<IAggregateFactory, AggregateFactory>();
            _serviceProvider = services.BuildServiceProvider();
            _serviceProvider.GetService<IEventListener>(); // Resolve to trigger subscription
            var factory = _serviceProvider.GetRequiredService<IAggregateFactory>();

            var customer = await factory.CreateAsync<CustomerAggregate>();

            // Act
            customer.RegisterCustomer("Test");

            // Assert
            Assert.That(TestMiddleware.BeforeHandleCount, Is.EqualTo(1));
        }

        [Test]
        public async Task Middleware_ShouldCallOnHandled()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IEventQueue, InMemoryEventQueue>();
            services.AddSingleton<IEventMiddleware, TestMiddleware>();
            services.AddSingleton<IHandler, SimpleCustomerCreatedHandler>();
            services.AddSingleton<IResolver>(sp => new Resolver(sp.GetServices<IHandler>()));
            services.AddSingleton<IEventDispatcher, EventDispatcher>();
            services.AddSingleton<IEventListener>(sp => 
                new EventListener(
                    sp.GetRequiredService<IEventQueue>(),
                    sp.GetRequiredService<IResolver>(),
                    sp.GetServices<IEventMiddleware>()));
            services.AddSingleton<IEventInterceptor>(sp => new EventInterceptor(sp.GetRequiredService<IEventDispatcher>()));
            services.AddSingleton<IAggregateFactory, AggregateFactory>();
            _serviceProvider = services.BuildServiceProvider();
            _serviceProvider.GetService<IEventListener>(); // Resolve to trigger subscription
            var factory = _serviceProvider.GetRequiredService<IAggregateFactory>();

            var customer = await factory.CreateAsync<CustomerAggregate>();

            // Act
            customer.RegisterCustomer("Test");

            // Assert
            Assert.That(TestMiddleware.AfterHandleCount, Is.EqualTo(1));
        }

        [Test]
        public async Task Middleware_OnDispatchingFalse_ShouldSkipDispatch()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IEventMiddleware>(new SkippingMiddleware("test"));
            services.AddDomainEvents(typeof(SimpleCustomerCreatedHandler).Assembly);
            _serviceProvider = services.BuildServiceProvider();
            var factory = _serviceProvider.GetRequiredService<IAggregateFactory>();

            var customer = await factory.CreateAsync<CustomerAggregate>();

            // Act
            customer.RegisterCustomer("Test");

            // Assert - handler should not be called
            Assert.That(SimpleCustomerCreatedHandler.HandleCount, Is.EqualTo(0));
        }
    }

    /// <summary>
    /// Test middleware that tracks method calls.
    /// </summary>
    public class TestMiddleware : IEventMiddleware
    {
        public static int BeforeDispatchCount = 0;
        public static int AfterDispatchCount = 0;
        public static int BeforeHandleCount = 0;
        public static int AfterHandleCount = 0;

        public Task<bool> OnDispatchingAsync(EventContext context)
        {
            BeforeDispatchCount++;
            return Task.FromResult(true);
        }

        public Task OnDispatchedAsync(EventContext context)
        {
            AfterDispatchCount++;
            return Task.CompletedTask;
        }

        public Task<bool> OnHandlingAsync(EventContext context)
        {
            BeforeHandleCount++;
            return Task.FromResult(true);
        }

        public Task OnHandledAsync(EventContext context)
        {
            AfterHandleCount++;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Test middleware that skips dispatching.
    /// Has a constructor with parameter to prevent auto-registration.
    /// </summary>
    public class SkippingMiddleware : IEventMiddleware
    {
        public SkippingMiddleware(string name)
        {
            // Constructor with parameter to prevent auto-registration
        }

        public Task<bool> OnDispatchingAsync(EventContext context)
        {
            return Task.FromResult(false);
        }

        public Task OnDispatchedAsync(EventContext context) => Task.CompletedTask;
        public Task<bool> OnHandlingAsync(EventContext context) => Task.FromResult(true);
        public Task OnHandledAsync(EventContext context) => Task.CompletedTask;
    }
}
