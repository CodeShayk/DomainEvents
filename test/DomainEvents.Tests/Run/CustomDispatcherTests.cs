using DomainEvents.Impl;
using DomainEvents.Tests.Aggregates;
using DomainEvents.Tests.Events;
using DomainEvents.Tests.Handlers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace DomainEvents.Tests.Run
{
    /// <summary>
    /// Tests for custom event dispatcher support.
    /// </summary>
    public class CustomDispatcherTests
    {
        [SetUp]
        public void Setup()
        {
            TestEventDispatcher.DispatchedEvents.Clear();
            SimpleCustomerCreatedHandler.HandleCount = 0;
            SimpleOrderReceivedHandler.HandleCount = 0;
        }

        [Test]
        public void AddDomainEvents_ShouldRegisterDefaultDispatcher()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDomainEvents(typeof(SimpleCustomerCreatedHandler).Assembly);
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var dispatcher = serviceProvider.GetService<IEventDispatcher>();

            // Assert
            Assert.That(dispatcher, Is.Not.Null);
            Assert.That(dispatcher, Is.InstanceOf<EventDispatcher>());
        }

        [Test]
        public void AddDomainEventsWithDispatcher_Type_ShouldRegisterCustomDispatcher()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDomainEventsWithDispatcher<TestEventDispatcher>(typeof(SimpleCustomerCreatedHandler).Assembly);
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var dispatcher = serviceProvider.GetService<IEventDispatcher>();

            // Assert
            Assert.That(dispatcher, Is.Not.Null);
            Assert.That(dispatcher, Is.InstanceOf<TestEventDispatcher>());
        }

        [Test]
        public void AddDomainEventsWithDispatcher_Instance_ShouldRegisterCustomDispatcher()
        {
            // Arrange
            var services = new ServiceCollection();
            var customDispatcher = new TestEventDispatcher();
            services.AddDomainEventsWithDispatcher(customDispatcher, typeof(SimpleCustomerCreatedHandler).Assembly);
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var dispatcher = serviceProvider.GetService<IEventDispatcher>();

            // Assert
            Assert.That(dispatcher, Is.Not.Null);
            Assert.That(dispatcher, Is.EqualTo(customDispatcher));
        }

        [Test]
        public async Task CustomDispatcher_ShouldDispatchEvents()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDomainEventsWithDispatcher<TestEventDispatcher>(typeof(SimpleCustomerCreatedHandler).Assembly);
            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<IAggregateFactory>();

            var customer = await factory.CreateAsync<CustomerAggregate>();

            // Act
            customer.RegisterCustomer("Test Customer");

            // Assert - custom dispatcher should have received the event
            Assert.That(TestEventDispatcher.DispatchedEvents.Count, Is.EqualTo(1));
            Assert.That(TestEventDispatcher.DispatchedEvents[0], Is.InstanceOf<CustomerCreated>());
        }

        [Test]
        public async Task DefaultInterceptor_ShouldWorkWithCustomDispatcher()
        {
            // Arrange - custom dispatcher only tracks, doesn't forward to handlers
            // This tests that the interceptor correctly uses the custom dispatcher
            var services = new ServiceCollection();
            services.AddDomainEventsWithDispatcher<TestEventDispatcher>(typeof(SimpleCustomerCreatedHandler).Assembly);
            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<IAggregateFactory>();

            var customer = await factory.CreateAsync<CustomerAggregate>();

            // Act
            customer.RegisterCustomer("Test Customer");

            // Assert - custom dispatcher should have received the event
            Assert.That(TestEventDispatcher.DispatchedEvents.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task DefaultDispatcher_ShouldWorkWithoutCustomDispatcher()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDomainEvents(typeof(SimpleCustomerCreatedHandler).Assembly);
            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<IAggregateFactory>();

            var customer = await factory.CreateAsync<CustomerAggregate>();

            // Act
            customer.RegisterCustomer("Test Customer");

            // Assert
            Assert.That(SimpleCustomerCreatedHandler.HandleCount, Is.EqualTo(1));
        }

        [Test]
        public void AggregateFactory_WithServiceProvider_ShouldResolveDispatcher()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDomainEventsWithDispatcher<TestEventDispatcher>(typeof(SimpleCustomerCreatedHandler).Assembly);
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var factory = serviceProvider.GetRequiredService<IAggregateFactory>();

            // Assert
            Assert.That(factory, Is.Not.Null);
            Assert.That(factory, Is.InstanceOf<AggregateFactory>());
        }
    }

    /// <summary>
    /// Test event dispatcher that tracks dispatched events.
    /// </summary>
    public class TestEventDispatcher : IEventDispatcher
    {
        public static List<object> DispatchedEvents { get; } = new();

        public void Dispatch(object @event)
        {
            if (@event != null)
            {
                DispatchedEvents.Add(@event);
            }
        }

        public Task DispatchAsync(object @event)
        {
            Dispatch(@event);
            return Task.CompletedTask;
        }
    }
}
