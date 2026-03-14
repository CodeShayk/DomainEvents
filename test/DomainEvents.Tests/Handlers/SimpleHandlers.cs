using System.Threading.Tasks;
using DomainEvents.Tests.Events;

namespace DomainEvents.Tests.Handlers
{
    /// <summary>
    /// Simple handler for DI tests without constructor dependencies.
    /// </summary>
    public class SimpleCustomerCreatedHandler : IHandler<CustomerCreated>
    {
        public static int HandleCount { get; set; }

        public Task HandleAsync(CustomerCreated @event)
        {
            HandleCount++;
            Console.WriteLine($"Simple handler: Customer created: {@event.Name}");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Simple handler for DI tests without constructor dependencies.
    /// </summary>
    public class SimpleOrderReceivedHandler : IHandler<OrderReceived>
    {
        public static int HandleCount { get; set; }

        public Task HandleAsync(OrderReceived @event)
        {
            HandleCount++;
            Console.WriteLine($"Simple handler: Order received: {@event.OrderNo}");
            return Task.CompletedTask;
        }
    }
}
