namespace DomainEvents.Tests.Aggregates
{
    public interface IOrderService
    {
        void DoSomethingWithOrder(string orderNo);
        int Counter { get; }
    }

    public class OrderService : IOrderService
    {
        public int Counter { get; private set; } = 0;
        public void DoSomethingWithOrder(string orderNo)
        {
            Counter++;
            Console.WriteLine($"OrderService is doing something with order: {orderNo}");
        }
    }
}