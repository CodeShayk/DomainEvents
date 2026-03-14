using System.Threading.Tasks;
using DomainEvents.Tests.Events;

namespace DomainEvents.Tests.Handlers
{
    public class OrderReceivedHandler : IHandler<OrderReceived>
    {
        private readonly Dictionary<IDomainEvent, Type> _handlerResult;

        public OrderReceivedHandler(Dictionary<IDomainEvent, Type> handlerResult)
        {
            _handlerResult = handlerResult;
        }

        public Task HandleAsync(OrderReceived @event)
        {
            Console.WriteLine($"Order received: {@event.OrderNo}");
            _handlerResult.Add(@event, this.GetType());
            return Task.CompletedTask;
        }
    }
}