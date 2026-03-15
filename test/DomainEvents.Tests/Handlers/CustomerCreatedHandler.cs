using System.Threading.Tasks;
using DomainEvents.Tests.Events;

namespace DomainEvents.Tests.Handlers
{
    public class CustomerCreatedHandler : IHandler<CustomerCreated>
    {
        private readonly Dictionary<IDomainEvent, Type> _handlerResult;

        public CustomerCreatedHandler(Dictionary<IDomainEvent, Type> handlerResult)
        {
            _handlerResult = handlerResult;
        }

        public Task HandleAsync(CustomerCreated @event)
        {
            Console.WriteLine($"Customer created: {@event.Name}");
            _handlerResult.Add(@event, this.GetType());
            return Task.CompletedTask;
        }
    }
}