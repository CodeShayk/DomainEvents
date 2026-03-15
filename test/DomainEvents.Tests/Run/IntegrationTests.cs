using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Tests for Event Queue functionality.
    /// </summary>
    public class QueueTests
    {
        [Test]
        public void InMemoryQueue_Enqueue_ShouldAddToQueue()
        {
            var queue = new InMemoryEventQueue();
            var context = new EventContext(new CustomerCreated { Name = "Test" });

            queue.EnqueueAsync(context).Wait();

            Assert.That(queue.Count, Is.EqualTo(1));
        }

        [Test]
        public void InMemoryQueue_Dequeue_ShouldRemoveFromQueue()
        {
            var queue = new InMemoryEventQueue();
            var context = new EventContext(new CustomerCreated { Name = "Test" });
            queue.EnqueueAsync(context).Wait();

            var dequeued = queue.DequeueAsync().Result;

            Assert.That(queue.Count, Is.EqualTo(0));
            Assert.That(dequeued, Is.Not.Null);
            Assert.That(((CustomerCreated)dequeued.Event).Name, Is.EqualTo("Test"));
        }

        [Test]
        public void InMemoryQueue_DequeueEmpty_ShouldReturnNull()
        {
            var queue = new InMemoryEventQueue();

            var dequeued = queue.DequeueAsync().Result;

            Assert.That(dequeued, Is.Null);
        }

        [Test]
        public void InMemoryQueue_PeekAll_ShouldReturnAll()
        {
            var queue = new InMemoryEventQueue();
            queue.EnqueueAsync(new EventContext(new CustomerCreated { Name = "Test1" })).Wait();
            queue.EnqueueAsync(new EventContext(new CustomerCreated { Name = "Test2" })).Wait();

            var all = queue.PeekAll();

            Assert.That(all.Count, Is.EqualTo(2));
        }

        [Test]
        public void InMemoryQueue_Clear_ShouldRemoveAll()
        {
            var queue = new InMemoryEventQueue();
            queue.EnqueueAsync(new EventContext(new CustomerCreated { Name = "Test1" })).Wait();
            queue.EnqueueAsync(new EventContext(new CustomerCreated { Name = "Test2" })).Wait();

            queue.Clear();

            Assert.That(queue.Count, Is.EqualTo(0));
        }
    }

    /// <summary>
    /// Tests for EventContext.
    /// </summary>
    public class EventContextTests
    {
        [Test]
        public void EventContext_ShouldStoreEvent()
        {
            var @event = new CustomerCreated { Name = "Test" };
            var context = new EventContext(@event);

            Assert.That(context.Event, Is.EqualTo(@event));
            Assert.That(context.EventType, Is.EqualTo(typeof(CustomerCreated)));
            Assert.That(context.Timestamp, Is.Not.EqualTo(default(DateTime)));
            Assert.That(context.Items, Is.Not.Null);
            Assert.That(context.IsHandled, Is.False);
            Assert.That(context.IsDispatched, Is.False);
        }

        [Test]
        public void EventContext_ShouldAllowSettingProperties()
        {
            var context = new EventContext(new CustomerCreated { Name = "Test" });

            context.IsHandled = true;
            context.IsDispatched = true;
            context.Items["Key"] = "Value";

            Assert.That(context.IsHandled, Is.True);
            Assert.That(context.IsDispatched, Is.True);
            Assert.That(context.Items["Key"], Is.EqualTo("Value"));
        }
    }

    /// <summary>
    /// Tests for custom dispatcher.
    /// </summary>
    public class DispatcherRegistrationTests
    {
        [Test]
        public void AddDomainEventsWithDispatcher_ShouldRegisterCustomDispatcher()
        {
            var services = new ServiceCollection();
            services.AddDomainEventsWithDispatcher<CustomTestDispatcher>(typeof(SimpleCustomerCreatedHandler).Assembly);
            var provider = services.BuildServiceProvider();

            var dispatcher = provider.GetService<IEventDispatcher>();

            Assert.That(dispatcher, Is.InstanceOf<CustomTestDispatcher>());
        }

        [Test]
        public void AddDomainEventsWithDispatcherInstance_ShouldRegisterInstance()
        {
            var services = new ServiceCollection();
            var customDispatcher = new CustomTestDispatcher();
            services.AddDomainEventsWithDispatcher(customDispatcher, typeof(SimpleCustomerCreatedHandler).Assembly);
            var provider = services.BuildServiceProvider();

            var dispatcher = provider.GetService<IEventDispatcher>();

            Assert.That(dispatcher, Is.EqualTo(customDispatcher));
        }

        public class CustomTestDispatcher : IEventDispatcher
        {
            public void Dispatch(object @event) { }
            public Task DispatchAsync(object @event) => Task.CompletedTask;
        }
    }

    /// <summary>
    /// Integration tests for full flow.
    /// </summary>
    public class IntegrationTests
    {
        private IServiceProvider _serviceProvider;

        [SetUp]
        public void Setup()
        {
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
        public async Task FullFlow_WithQueue_ShouldWork()
        {
            var services = new ServiceCollection();
            services.AddDomainEvents(typeof(SimpleCustomerCreatedHandler).Assembly);
            _serviceProvider = services.BuildServiceProvider();

            var factory = _serviceProvider.GetRequiredService<IAggregateFactory>();

            var customer = await factory.CreateAsync<CustomerAggregate>();
            customer.RegisterCustomer("Integration Test");

            Assert.That(SimpleCustomerCreatedHandler.HandleCount, Is.EqualTo(1));
        }

        [Test]
        public async Task Publisher_WithoutProxy_ShouldDispatchDirectly()
        {
            var services = new ServiceCollection();
            services.AddDomainEvents(typeof(SimpleCustomerCreatedHandler).Assembly);
            _serviceProvider = services.BuildServiceProvider();

            var publisher = _serviceProvider.GetRequiredService<IPublisher>();

            await publisher.RaiseAsync(new CustomerCreated { Name = "Direct Publish" });

            Assert.That(SimpleCustomerCreatedHandler.HandleCount, Is.EqualTo(1));
        }

        [Test]
        public async Task MultipleEvents_ShouldProcessAll()
        {
            var services = new ServiceCollection();
            services.AddDomainEvents(typeof(SimpleCustomerCreatedHandler).Assembly);
            _serviceProvider = services.BuildServiceProvider();

            var publisher = _serviceProvider.GetRequiredService<IPublisher>();

            await publisher.RaiseAsync(new CustomerCreated { Name = "Test1" });
            await publisher.RaiseAsync(new CustomerCreated { Name = "Test2" });

            Assert.That(SimpleCustomerCreatedHandler.HandleCount, Is.EqualTo(2));
        }

        [Test]
        public void Resolver_ShouldResolveMultipleHandlers()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IHandler>(new FirstHandler());
            services.AddSingleton<IHandler>(new SecondHandler());
            services.AddSingleton<IResolver>(sp => new Resolver(sp.GetServices<IHandler>()));
            _serviceProvider = services.BuildServiceProvider();

            var resolver = _serviceProvider.GetRequiredService<IResolver>();

            var handlers = resolver.ResolveAsync<MultiEvent>().Result.ToList();

            Assert.That(handlers.Count, Is.EqualTo(2));
        }

        public class MultiEvent : IDomainEvent { }

        public class FirstHandler : IHandler<MultiEvent>
        {
            public Task HandleAsync(MultiEvent @event) => Task.CompletedTask;
        }

        public class SecondHandler : IHandler<MultiEvent>
        {
            public Task HandleAsync(MultiEvent @event) => Task.CompletedTask;
        }
    }
}
