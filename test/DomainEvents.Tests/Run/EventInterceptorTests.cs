using System.Diagnostics;
using DomainEvents.Impl;
using DomainEvents.Tests.Aggregates;
using DomainEvents.Tests.Events;
using DomainEvents.Tests.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace DomainEvents.Tests.Run
{
    /// <summary>
    /// Unit tests for EventInterceptor.
    /// </summary>
    public class EventInterceptorTests
    {
        private IServiceProvider _serviceProvider;
        private IResolver _resolver;
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
            services.AddSingleton<IResolver>(_ => new Resolver(handlers));
            services.AddSingleton<IEventDispatcher>(sp => new EventDispatcher(sp.GetRequiredService<IResolver>()));
            services.AddSingleton<IEventInterceptor, EventInterceptor>();
            services.AddSingleton<IAggregateFactory, AggregateFactory>();
            
            _serviceProvider = services.BuildServiceProvider();
            _resolver = _serviceProvider.GetRequiredService<IResolver>();
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
        public void Intercept_WithNullLogger_ShouldNotThrow()
        {
            var dispatcher = _serviceProvider.GetRequiredService<IEventDispatcher>();
            var interceptor = new EventInterceptor(dispatcher, null);
            var factory = new TestAggregateFactory(interceptor);
            var customer = factory.CreateAsync<CustomerAggregate>().Result;

            Assert.DoesNotThrow(() => customer.RegisterCustomer("Test"));
        }

        [Test]
        public void Intercept_WithLogger_ShouldLog()
        {
            var dispatcher = _serviceProvider.GetRequiredService<IEventDispatcher>();
            var logger = new MockLogger<EventInterceptor>();
            var interceptor = new EventInterceptor(dispatcher, logger);
            var factory = new TestAggregateFactory(interceptor);

            var customer = factory.CreateAsync<CustomerAggregate>().Result;

            customer.RegisterCustomer("Test");

            Assert.That(logger.LogMessages.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Intercept_WithNonResolver_ShouldNotDispatch()
        {
            var mockDispatcher = new MockEventDispatcher();
            var interceptor = new EventInterceptor(mockDispatcher);
            var factory = new TestAggregateFactory(interceptor);

            var customer = factory.CreateAsync<CustomerAggregate>().Result;

            customer.RegisterCustomer("Test");

            Assert.That(_handlerResult.Count, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WithNullDispatcher_ShouldNotThrow()
        {
            Assert.DoesNotThrow(() => new EventInterceptor(null));
        }
    }

    /// <summary>
    /// Mock resolver for testing.
    /// </summary>
    public class MockResolver : IResolver
    {
        public Task<IEnumerable<IHandler<T>>> ResolveAsync<T>() where T : IDomainEvent
        {
            return Task.FromResult(Enumerable.Empty<IHandler<T>>());
        }
    }

    /// <summary>
    /// Mock event dispatcher for testing.
    /// </summary>
    public class MockEventDispatcher : IEventDispatcher
    {
        public void Dispatch(object @event) { }
        public Task DispatchAsync(object @event) => Task.CompletedTask;
    }

    /// <summary>
    /// Mock logger for testing.
    /// </summary>
    public class MockLogger<T> : ILogger<T>
    {
        public List<string> LogMessages { get; } = new List<string>();

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LogMessages.Add(formatter(state, exception));
        }
    }

    /// <summary>
    /// Test AggregateFactory wrapper for testing with custom interceptor.
    /// </summary>
    public class TestAggregateFactory
    {
        private readonly Castle.DynamicProxy.ProxyGenerator _proxyGenerator = new();
        private readonly EventInterceptor _interceptor;

        public TestAggregateFactory(IEventDispatcher dispatcher)
        {
            _interceptor = new EventInterceptor(dispatcher);
        }

        public TestAggregateFactory(EventInterceptor interceptor)
        {
            _interceptor = interceptor;
        }

        public Task<T> CreateAsync<T>(params object[] constructorArguments) where T : Aggregate
        {
            var proxy = _proxyGenerator.CreateClassProxy<T>(_interceptor);
            return Task.FromResult(proxy);
        }
    }
}
