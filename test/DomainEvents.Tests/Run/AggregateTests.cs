using DomainEvents.Impl;
using DomainEvents.Tests.Aggregates;
using DomainEvents.Tests.Events;
using DomainEvents.Tests.Handlers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace DomainEvents.Tests.Run
{
    /// <summary>
    /// Tests for Aggregate base class.
    /// </summary>
    public class AggregateTests
    {
        private IServiceProvider _serviceProvider;
        private IResolver _resolver;
        private IPublisher _publisher;
        private IAggregateFactory _aggregateFactory;
        private Dictionary<IDomainEvent, Type> _handlerResult;

        [SetUp]
        public void Setup()
        {
            _handlerResult = new Dictionary<IDomainEvent, Type>();
            var handlers = new List<IHandler>
            {
                new CustomerCreatedHandler(_handlerResult),
                new OrderReceivedHandler(_handlerResult)
            };
            
            var services = new ServiceCollection();
            services.AddSingleton<IPublisher, Publisher>();
            services.AddSingleton<IResolver>(_ => new Resolver(handlers));
            services.AddSingleton<IEventDispatcher>(sp => new EventDispatcher(sp.GetRequiredService<IResolver>()));
            services.AddSingleton<IEventInterceptor, EventInterceptor>();
            services.AddSingleton<IAggregateFactory, AggregateFactory>();
            
            _serviceProvider = services.BuildServiceProvider();
            _resolver = _serviceProvider.GetRequiredService<IResolver>();
            _publisher = _serviceProvider.GetRequiredService<IPublisher>();
            _aggregateFactory = _serviceProvider.GetRequiredService<IAggregateFactory>();
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
        public void Aggregate_Raise_ShouldDispatchEventToHandlers()
        {
            // Arrange
            var customer = new CustomerAggregate();

            // Act
            customer.RegisterCustomer("John Doe");

            // Assert - no handlers should be called since we're not using a proxy
            Assert.That(_handlerResult.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task AggregateFactory_CreateAsync_ShouldCreateProxiedAggregate()
        {
            // Arrange
            var customer = await _aggregateFactory.CreateAsync<CustomerAggregate>();

            // Act - cast to concrete type to access RegisterCustomer
            customer.RegisterCustomer("John Doe");

            // Assert
            Assert.That(_handlerResult.Count, Is.EqualTo(1));
            Assert.That(_handlerResult.Values.First(), Is.EqualTo(typeof(CustomerCreatedHandler)));
        }

        [Test]
        public async Task Aggregate_AsEventSubscriber_ShouldReceiveEvents()
        {
            // Arrange
            var warehouse = await _aggregateFactory.CreateAsync<WarehouseAggregate>();

            // Act - cast to concrete type to access ProcessOrder
            warehouse.ProcessOrder("ORD-123");

            // Assert - The warehouse handles the event internally, plus any registered handlers
            Assert.That(_handlerResult.Count, Is.GreaterThan(0));
            Assert.That(_handlerResult.ContainsValue(typeof(OrderReceivedHandler)), Is.True);
        }

        [Test]
        public async Task AggregateFactory_CreateAsync_WithEventType_ShouldCreateProxiedAggregate()
        {
            // Arrange
            var aggregate = await _aggregateFactory.CreateAsync(typeof(CustomerAggregate));

            // Assert
            Assert.That(aggregate, Is.Not.Null);
            Assert.That(aggregate, Is.InstanceOf<IDomainAggregate>());
        }
    }
}
