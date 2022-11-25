using DomainEvents.Tests.Events;

namespace DomainEvents.Tests.Handlers
{
    public class OrderReceivedHandler : IHandler<OrderReceived>
    {
        private readonly Dictionary<IDomainEvent, Type> _HandlerResult;

        public OrderReceivedHandler(Dictionary<IDomainEvent, Type> handlerResult)
        {
            _HandlerResult = handlerResult;
        }

        public Task HandleAsync(OrderReceived args)
        {
            Console.WriteLine($"Order received: {args.OrderNo}");
            _HandlerResult.Add(args, this.GetType());

            return Task.CompletedTask;
        }
    }
}