using DomainEvents.Impl;
using DomainEvents.Tests.Events;
using DomainEvents.Tests.Handlers;
using NUnit.Framework;

namespace DomainEvents.Tests.Run
{
    public class DomainTests
    {
        private Publisher _publisher;
        private Dictionary<IDomainEvent, Type> _handlerResult;

        [SetUp]
        public void Setup()
        {
            _handlerResult = new Dictionary<IDomainEvent, Type>();
            _publisher = new Publisher(new Resolver(
                new List<IHandler> { new CustomerCreatedHandler(_handlerResult),
                new OrderReceivedHandler(_handlerResult) })
             );
        }

        [Test]
        public async Task PublishCustomerCreatedTest()
        {
            var @event = new CustomerCreated { Name = "Ninja Sha!4h" };
            await _publisher.RaiseAsync(@event);

            Assert.That(_handlerResult.Count, Is.EqualTo(1));
            Assert.That(_handlerResult.ContainsKey(@event), Is.True);
            Assert.That(_handlerResult.ContainsValue(typeof(CustomerCreatedHandler)), Is.True);
        }

        [Test]
        public async Task PublishOrderReceivedTest()
        {
            var @event = new OrderReceived { OrderNo = "23451GHY0WQ" };
            await _publisher.RaiseAsync(@event);

            Assert.That(_handlerResult.Count, Is.EqualTo(1));
            Assert.That(_handlerResult.ContainsKey(@event), Is.True);
            Assert.That(_handlerResult.ContainsValue(typeof(OrderReceivedHandler)), Is.True);
        }
    }
}