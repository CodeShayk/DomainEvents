using DomainEvents.Impl;
using DomainEvents.Tests.Aggregates;
using DomainEvents.Tests.Events;
using DomainEvents.Tests.Handlers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace DomainEvents.Tests.Run
{
    /// <summary>
    /// Integration tests for IAggregateFactory.
    /// </summary>
    public class AggregateFactoryIntegrationTests
    {
        private IServiceProvider _serviceProvider;
        private IAggregateFactory _aggregateFactory;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.AddDomainEvents(typeof(SimpleCustomerCreatedHandler).Assembly);
            _serviceProvider = services.BuildServiceProvider();
            _aggregateFactory = _serviceProvider.GetRequiredService<IAggregateFactory>();

            SimpleCustomerCreatedHandler.HandleCount = 0;
            SimpleOrderReceivedHandler.HandleCount = 0;
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
        public async Task CreateAsync_ShouldCreateProxiedCustomerAggregate()
        {
            // Arrange & Act
            var customer = await _aggregateFactory.CreateAsync<CustomerAggregate>();

            // Assert
            Assert.That(customer, Is.Not.Null);
            Assert.That(customer, Is.InstanceOf<CustomerAggregate>());
        }

        [Test]
        public async Task CreateAsync_ShouldCreateProxiedWarehouseAggregate()
        {
            // Arrange & Act
            var warehouse = await _aggregateFactory.CreateAsync<WarehouseAggregate>();

            // Assert
            Assert.That(warehouse, Is.Not.Null);
            Assert.That(warehouse, Is.InstanceOf<WarehouseAggregate>());
        }

        [Test]
        public async Task CreateAsync_CustomerAggregate_RaiseShouldDispatchEvents()
        {
            // Arrange
            var handlerResult = new Dictionary<IDomainEvent, Type>();
            var services = new ServiceCollection();
            services.AddSingleton<IEventQueue, InMemoryEventQueue>();
            services.AddSingleton<IResolver>(_ =>
            {
                var handlers = new List<IHandler> { new CustomerCreatedHandler(handlerResult) };
                return new Resolver(handlers);
            });
            services.AddSingleton<IEventDispatcher>(sp => new EventDispatcher(sp.GetRequiredService<IResolver>(), sp.GetRequiredService<IEventQueue>()));
            services.AddSingleton<IEventListener>(sp => new EventListener(sp.GetRequiredService<IEventQueue>(), sp.GetRequiredService<IResolver>()));
            services.AddSingleton<IEventInterceptor, EventInterceptor>();
            services.AddSingleton<IAggregateFactory, AggregateFactory>();
            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<IAggregateFactory>();

            // Act
            var customer = await factory.CreateAsync<CustomerAggregate>();
            customer.RegisterCustomer("Integration Test Customer");

            // Assert
            Assert.That(handlerResult.Count, Is.EqualTo(1));
            Assert.That(handlerResult.Values.First(), Is.EqualTo(typeof(CustomerCreatedHandler)));
        }

        [Test]
        public async Task CreateAsync_WarehouseAggregate_RaiseShouldDispatchEvents()
        {
            // Arrange
            var handlerResult = new Dictionary<IDomainEvent, Type>();
            var services = new ServiceCollection();
            services.AddSingleton<IEventQueue, InMemoryEventQueue>();
            services.AddSingleton<IResolver>(_ =>
            {
                var handlers = new List<IHandler> { new OrderReceivedHandler(handlerResult) };
                return new Resolver(handlers);
            });
            services.AddSingleton<IEventDispatcher>(sp => new EventDispatcher(sp.GetRequiredService<IResolver>(), sp.GetRequiredService<IEventQueue>()));
            services.AddSingleton<IEventListener>(sp => new EventListener(sp.GetRequiredService<IEventQueue>(), sp.GetRequiredService<IResolver>()));
            services.AddSingleton<IEventInterceptor, EventInterceptor>();
            services.AddSingleton<IAggregateFactory, AggregateFactory>();
            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<IAggregateFactory>();

            // Act
            var warehouse = await factory.CreateAsync<WarehouseAggregate>();
            warehouse.ProcessOrder("ORD-123");

            // Assert
            Assert.That(handlerResult.Count, Is.EqualTo(1));
            Assert.That(handlerResult.Values.First(), Is.EqualTo(typeof(OrderReceivedHandler)));
        }

        [Test]
        public async Task CreateAsync_WithMultipleHandlers_ShouldDispatchToAll()
        {
            // Arrange
            var handlerResult = new Dictionary<IDomainEvent, Type>();
            var services = new ServiceCollection();
            services.AddSingleton<IEventQueue, InMemoryEventQueue>();
            services.AddSingleton<IResolver>(_ =>
            {
                var handlers = new List<IHandler>
                {
                    new CustomerCreatedHandler(handlerResult),
                    new SimpleCustomerCreatedHandler()
                };
                return new Resolver(handlers);
            });
            services.AddSingleton<IEventDispatcher>(sp => new EventDispatcher(sp.GetRequiredService<IResolver>(), sp.GetRequiredService<IEventQueue>()));
            services.AddSingleton<IEventListener>(sp => new EventListener(sp.GetRequiredService<IEventQueue>(), sp.GetRequiredService<IResolver>()));
            services.AddSingleton<IEventInterceptor, EventInterceptor>();
            services.AddSingleton<IAggregateFactory, AggregateFactory>();
            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<IAggregateFactory>();

            // Act
            var customer = await factory.CreateAsync<CustomerAggregate>();
            customer.RegisterCustomer("Multi Handler Test");

            // Assert
            Assert.That(handlerResult.Count, Is.EqualTo(1));
            Assert.That(SimpleCustomerCreatedHandler.HandleCount, Is.EqualTo(1));
        }

        [Test]
        public async Task CreateAsync_NonGeneric_ShouldCreateProxiedAggregate()
        {
            // Arrange & Act
            var aggregate = await _aggregateFactory.CreateAsync(typeof(CustomerAggregate));

            // Assert
            Assert.That(aggregate, Is.Not.Null);
            Assert.That(aggregate, Is.InstanceOf<IDomainAggregate>());
        }

        [Test]
        public async Task CreateAggregate_UsingServiceProvider_ShouldWork()
        {
            // Arrange
            var factory = _serviceProvider.GetRequiredService<IAggregateFactory>();

            // Act
            var customer = await factory.CreateAsync<CustomerAggregate>();

            // Assert
            Assert.That(customer, Is.Not.Null);
        }

        [Test]
        public async Task RaiseAsync_OnProxiedAggregate_ShouldInterceptAndDispatch()
        {
            // Arrange
            var handlerResult = new Dictionary<IDomainEvent, Type>();
            var services = new ServiceCollection();
            services.AddSingleton<IEventQueue, InMemoryEventQueue>();
            services.AddSingleton<IResolver>(_ =>
            {
                var handlers = new List<IHandler> { new CustomerCreatedHandler(handlerResult) };
                return new Resolver(handlers);
            });
            services.AddSingleton<IEventDispatcher>(sp => new EventDispatcher(sp.GetRequiredService<IResolver>(), sp.GetRequiredService<IEventQueue>()));
            services.AddSingleton<IEventListener>(sp => new EventListener(sp.GetRequiredService<IEventQueue>(), sp.GetRequiredService<IResolver>()));
            services.AddSingleton<IEventInterceptor, EventInterceptor>();
            services.AddSingleton<IAggregateFactory, AggregateFactory>();
            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<IAggregateFactory>();

            // Act
            var customer = await factory.CreateAsync<CustomerAggregate>();
            customer.RegisterCustomer("Async Test");

            // Assert
            Assert.That(handlerResult.Count, Is.EqualTo(1));
        }

        [Test]
        public void Aggregate_WithoutProxy_ShouldNotDispatchEvents()
        {
            // Arrange
            var handlerResult = new Dictionary<IDomainEvent, Type>();
            var handlers = new List<IHandler>
            {
                new CustomerCreatedHandler(handlerResult)
            };
            var resolver = new Resolver(handlers);
            var publisher = new Publisher(resolver);

            // Create aggregate without proxy
            var customer = new CustomerAggregate();

            // Act
            customer.RegisterCustomer("No Proxy Test");

            // Assert - no handlers should be called since we're not using a proxy
            Assert.That(handlerResult.Count, Is.EqualTo(0));
        }
    }
}
