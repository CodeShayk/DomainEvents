using DomainEvents.Impl;
using DomainEvents.Tests.Events;
using DomainEvents.Tests.Handlers;
using NUnit.Framework;

namespace DomainEvents.Tests.Run
{
    public class DomainTests
    {
        private Publisher _Publisher;
        private Dictionary<IDomainEvent, Type> _HandlerResult;

        [SetUp]
        public void Setup()
        {
            _HandlerResult = new Dictionary<IDomainEvent, Type>();
            _Publisher = new Publisher(new Resolver(
                new List<IHandle> { new CustomerCreatedHandler(_HandlerResult),
                new OrderReceivedHandler(_HandlerResult) })
             );
        }

        [Test]
        public async Task PublishCustomerCreatedTest()
        {
            var @event = new CustomerCreated { Name = "Ninja Sha!4h" };
            await _Publisher.RaiseAsync(@event);

            Assert.That(_HandlerResult.Count, Is.EqualTo(1));
            Assert.That(_HandlerResult.ContainsKey(@event), Is.True);
            Assert.That(_HandlerResult.ContainsValue(typeof(CustomerCreatedHandler)), Is.True);
        }

        [Test]
        public async Task PublishOrderReceivedTest()
        {
            var @event = new OrderReceived { OrderNo = "23451GHY0WQ" };
            await _Publisher.RaiseAsync(@event);

            Assert.That(_HandlerResult.Count, Is.EqualTo(1));
            Assert.That(_HandlerResult.ContainsKey(@event), Is.True);
            Assert.That(_HandlerResult.ContainsValue(typeof(OrderReceivedHandler)), Is.True);
        }
    }
}