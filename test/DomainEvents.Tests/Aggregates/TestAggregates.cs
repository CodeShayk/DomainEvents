using System.Threading.Tasks;
using DomainEvents.Tests.Events;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace DomainEvents.Tests.Aggregates
{
    /// <summary>
    /// Test aggregate that raises CustomerCreated events.
    /// </summary>
    public class CustomerAggregate : Aggregate
    {
        public CustomerAggregate() : base()
        {
        }

        public void RegisterCustomer(string name)
        {
            // Some business logic here...
            var @event = new CustomerCreated { Name = name };
            Raise(@event);
        }
    }
    public class OrderAggregate : Aggregate
    {
        private IOrderService service;
       
        public OrderAggregate(IOrderService service) : base()
        {
            this.service = service;
        }

        public void CreateOrder(string orderNo)
        {
            // Some business logic here...
            service.DoSomethingWithOrder(orderNo);

            var @event = new OrderReceived { OrderNo = orderNo };
            Raise(@event);
        }
    }

    /// <summary>
    /// Test aggregate that handles OrderCreated events and raises OrderProcessed events.
    /// </summary>
    public class WarehouseAggregate : Aggregate, ISubscribes<OrderReceived>
    {
        private readonly List<OrderReceived> _receivedOrders = new();

        public WarehouseAggregate() : base()
        {
        }

        public Task HandleAsync(OrderReceived @event)
        {
            _receivedOrders.Add(@event);
            Console.WriteLine($"Warehouse processed order: {@event.OrderNo}");
            return Task.CompletedTask;
        }

        public void ProcessOrder(string orderNo)
        {
            // Some business logic here...
            var @event = new OrderReceived { OrderNo = orderNo };
            Raise(@event);
        }

        public IReadOnlyList<OrderReceived> GetReceivedOrders() => _receivedOrders.AsReadOnly();
    }
}
