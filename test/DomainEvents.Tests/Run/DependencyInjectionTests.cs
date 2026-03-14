using DomainEvents.Impl;
using DomainEvents.Tests.Aggregates;
using DomainEvents.Tests.Events;
using DomainEvents.Tests.Handlers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace DomainEvents.Tests.Run
{
    /// <summary>
    /// Tests for Microsoft.Extensions.DependencyInjection extensions.
    /// </summary>
    public class DependencyInjectionTests
    {
        [SetUp]
        public void SetUp()
        {
            SimpleCustomerCreatedHandler.HandleCount = 0;
            SimpleOrderReceivedHandler.HandleCount = 0;
        }

        [Test]
        public void AddDomainEvents_ShouldRegisterPublisher()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddDomainEvents(typeof(SimpleCustomerCreatedHandler).Assembly);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var publisher = serviceProvider.GetService<IPublisher>();
            Assert.That(publisher, Is.Not.Null);
            Assert.That(publisher, Is.InstanceOf<Publisher>());
        }

        [Test]
        public void AddDomainEvents_ShouldRegisterResolver()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddDomainEvents(typeof(SimpleCustomerCreatedHandler).Assembly);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var resolver = serviceProvider.GetService<IResolver>();
            Assert.That(resolver, Is.Not.Null);
            Assert.That(resolver, Is.InstanceOf<Resolver>());
        }

        [Test]
        public void AddDomainEvents_ShouldRegisterAggregateFactory()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddDomainEvents(typeof(SimpleCustomerCreatedHandler).Assembly);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var aggregateFactory = serviceProvider.GetService<IAggregateFactory>();
            Assert.That(aggregateFactory, Is.Not.Null);
        }

        [Test]
        public void AddDomainEvents_ShouldRegisterHandlers()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddDomainEvents(typeof(SimpleCustomerCreatedHandler).Assembly);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var handlers = serviceProvider.GetServices<IHandler>();
            Assert.That(handlers.Count(), Is.GreaterThan(0));
            Assert.That(handlers.Any(h => h is SimpleCustomerCreatedHandler), Is.True);
            Assert.That(handlers.Any(h => h is SimpleOrderReceivedHandler), Is.True);
        }

        [Test]
        public void AddDomainEvents_ResolverShouldResolveHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDomainEvents(typeof(SimpleCustomerCreatedHandler).Assembly);
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var resolver = serviceProvider.GetRequiredService<IResolver>();
            var customerHandlers = resolver.ResolveAsync<CustomerCreated>().Result;

            // Assert
            Assert.That(customerHandlers.Count(), Is.EqualTo(1));
            Assert.That(customerHandlers.First(), Is.InstanceOf<SimpleCustomerCreatedHandler>());
        }

        [Test]
        public void AddDomainEvents_FullScenario_WithPublisher()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDomainEvents(typeof(SimpleCustomerCreatedHandler).Assembly);
            var serviceProvider = services.BuildServiceProvider();

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            // Act
            var customerEvent = new CustomerCreated { Name = "Test Customer" };
            publisher.RaiseAsync(customerEvent).Wait();

            // Assert
            Assert.That(SimpleCustomerCreatedHandler.HandleCount, Is.EqualTo(1));
        }

        [Test]
        public void AddDomainEvents_WithCallingAssembly_ShouldScanCallingAssembly()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddDomainEvents();
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var handlers = serviceProvider.GetServices<IHandler>();
            Assert.That(handlers.Count(), Is.GreaterThan(0));
        }

        [Test]
        public void AddDomainEvents_WithNoAssemblies_ShouldThrow()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => services.AddDomainEvents(Array.Empty<System.Reflection.Assembly>()));
        }
    }
}
