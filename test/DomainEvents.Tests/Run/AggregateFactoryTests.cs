using DomainEvents.Impl;
using DomainEvents.Tests.Aggregates;
using DomainEvents.Tests.Events;
using DomainEvents.Tests.Handlers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace DomainEvents.Tests.Run
{
    /// <summary>
    /// Unit tests for AggregateFactory.
    /// </summary>
    public class AggregateFactoryTests
    {
        private IServiceProvider _serviceProvider;
        private IAggregateFactory _factory;
        private Dictionary<IDomainEvent, Type> _handlerResult;

        [SetUp]
        public void Setup()
        {
            _handlerResult = new Dictionary<IDomainEvent, Type>();
            var services = new ServiceCollection();
            services.AddSingleton<IEventQueue, InMemoryEventQueue>();
            services.AddSingleton<IResolver>(_ =>
            {
                var handlers = new List<IHandler>
                {
                    new CustomerCreatedHandler(_handlerResult),
                    new OrderReceivedHandler(_handlerResult)
                };
                return new Resolver(handlers);
            });
            services.AddSingleton<IEventDispatcher>(sp => new EventDispatcher(sp.GetRequiredService<IResolver>(), sp.GetRequiredService<IEventQueue>()));
            services.AddSingleton<IEventListener>(sp => new EventListener(sp.GetRequiredService<IEventQueue>(), sp.GetRequiredService<IResolver>()));
            services.AddSingleton<IEventInterceptor, EventInterceptor>();
            services.AddSingleton<IAggregateFactory, AggregateFactory>();
            _serviceProvider = services.BuildServiceProvider();
            _factory = _serviceProvider.GetRequiredService<IAggregateFactory>();
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
        public async Task CreateAsync_ShouldReturnNonNull()
        {
            // Act
            var aggregate = await _factory.CreateAsync<CustomerAggregate>();

            // Assert
            Assert.That(aggregate, Is.Not.Null);
        }

        [Test]
        public async Task CreateAsync_ShouldReturnCorrectType()
        {
            // Act
            var aggregate = await _factory.CreateAsync<CustomerAggregate>();

            // Assert
            Assert.That(aggregate, Is.InstanceOf<CustomerAggregate>());
            Assert.That(aggregate, Is.InstanceOf<IDomainAggregate>());
        }

        [Test]
        public async Task CreateAsync_NonGeneric_ShouldReturnNonNull()
        {
            // Act
            var aggregate = await _factory.CreateAsync(typeof(CustomerAggregate));

            // Assert
            Assert.That(aggregate, Is.Not.Null);
            Assert.That(aggregate, Is.InstanceOf<IDomainAggregate>());
        }

        [Test]
        public async Task CreateAsync_ProxiedAggregate_RaiseShouldDispatchEvents()
        {
            // Arrange
            var customer = await _factory.CreateAsync<CustomerAggregate>();

            // Act
            customer.RegisterCustomer("Factory Test");

            // Assert
            Assert.That(_handlerResult.Count, Is.EqualTo(1));
            Assert.That(_handlerResult.Values.First(), Is.EqualTo(typeof(CustomerCreatedHandler)));
        }

        [Test]
        public async Task CreateAsync_ProxiedWarehouseAggregate_ShouldHandleEvents()
        {
            // Arrange
            var warehouse = await _factory.CreateAsync<WarehouseAggregate>();

            // Act
            warehouse.ProcessOrder("ORD-FACTORY");

            // Assert - warehouse handles internally and external handlers receive
            Assert.That(_handlerResult.Count, Is.GreaterThan(0));
        }

        [Test]
        public async Task CreateAsync_MultipleInstances_ShouldBeIndependent()
        {
            // Arrange
            var customer1 = await _factory.CreateAsync<CustomerAggregate>();
            var customer2 = await _factory.CreateAsync<CustomerAggregate>();

            // Act
            customer1.RegisterCustomer("Customer 1");
            customer2.RegisterCustomer("Customer 2");

            // Assert
            Assert.That(_handlerResult.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task CreateAsync_WithNullConstructorArgs_ShouldWork()
        {
            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _factory.CreateAsync<CustomerAggregate>(null));
        }

        [Test]
        public async Task CreateFromInstanceAsync_ShouldReturnProxiedInstance()
        {
            // Arrange
            var original = new CustomerAggregate();

            // Act
            var proxied = await _factory.CreateFromInstanceAsync(original);

            // Assert
            Assert.That(proxied, Is.Not.Null);
            Assert.That(proxied, Is.InstanceOf<CustomerAggregate>());
            Assert.That(proxied, Is.InstanceOf<IDomainAggregate>());
        }

        [Test]
        public async Task CreateFromInstanceAsync_ProxiedInstance_ShouldDispatchEvents()
        {
            // Arrange
            var original = new CustomerAggregate();
            var proxied = await _factory.CreateFromInstanceAsync(original);

            // Act
            proxied.RegisterCustomer("FromInstance Test");

            // Assert
            Assert.That(_handlerResult.Count, Is.EqualTo(1));
            Assert.That(_handlerResult.Values.First(), Is.EqualTo(typeof(CustomerCreatedHandler)));
        }

        [Test]
        public async Task CreateFromInstanceAsync_NonGeneric_ShouldReturnProxiedInstance()
        {
            // Arrange
            var original = new CustomerAggregate();

            // Act
            var proxied = await _factory.CreateFromInstanceAsync<CustomerAggregate>(original);

            // Assert
            Assert.That(proxied, Is.Not.Null);
            Assert.That(proxied, Is.InstanceOf<IDomainAggregate>());
        }

        [Test]
        public async Task CreateFromInstanceAsync_ProxiedWarehouse_ShouldHandleEvents()
        {
            // Arrange
            var original = new WarehouseAggregate();
            var proxied = await _factory.CreateFromInstanceAsync(original);

            // Act
            proxied.ProcessOrder("ORD-INSTANCE");

            // Assert
            Assert.That(_handlerResult.Count, Is.GreaterThan(0));
        }

        [Test]
        public async Task CreateFromInstanceAsync_MultipleProxiesFromSameInstance_ShouldBothDispatchEvents()
        {
            // Arrange
            var original = new WarehouseAggregate();

            // Act
            var proxy1 = await _factory.CreateFromInstanceAsync(original);
            var proxy2 = await _factory.CreateFromInstanceAsync(original);

            proxy1.ProcessOrder("PROXY-1");
            proxy2.ProcessOrder("PROXY-2");

            // Assert - both proxies dispatch events to handlers
            Assert.That(_handlerResult.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task CreateFromServiceProviderAsync_ShouldReturnProxiedInstance()
        {
            // Arrange - register aggregate with DI
            var services = new ServiceCollection();
            services.AddTransient<CustomerAggregate>();
            services.AddSingleton<IEventQueue, InMemoryEventQueue>();
            services.AddSingleton<IResolver>(_ =>
            {
                var handlers = new List<IHandler>();
                return new Resolver(handlers);
            });
            services.AddSingleton<IEventDispatcher>(sp => new EventDispatcher(sp.GetRequiredService<IResolver>(), sp.GetRequiredService<IEventQueue>()));
            var sp = services.BuildServiceProvider();

            var factory = new AggregateFactory(sp);

            // Act
            var proxied = await factory.CreateFromServiceProviderAsync<CustomerAggregate>();

            // Assert
            Assert.That(proxied, Is.Not.Null);
            Assert.That(proxied, Is.InstanceOf<CustomerAggregate>());
            Assert.That(proxied, Is.InstanceOf<IDomainAggregate>());
        }

        [Test]
        public async Task CreateAsync_DefaultConstructor_ShouldWork()
        {
            // Arrange & Act
            var proxied = await _factory.CreateAsync<CustomerAggregate>();

            // Assert
            Assert.That(proxied, Is.Not.Null);
            Assert.That(proxied, Is.InstanceOf<CustomerAggregate>());
        }
    }
}
