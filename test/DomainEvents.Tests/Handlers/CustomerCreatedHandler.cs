using DomainEvents.Tests.Events;

namespace DomainEvents.Tests.Handlers
{
    public class CustomerCreatedHandler : IHandler<CustomerCreated>
    {
        private readonly Dictionary<IDomainEvent, Type> _HandlerResult;

        public CustomerCreatedHandler(Dictionary<IDomainEvent, Type> handlerResult)
        {
            _HandlerResult = handlerResult;
        }

        public Task HandleAsync(CustomerCreated args)
        {
            Console.WriteLine($"Customer created: {args.Name}");
            _HandlerResult.Add(args, this.GetType());

            return Task.CompletedTask;
        }
    }
}