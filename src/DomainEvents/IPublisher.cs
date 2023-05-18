using System.Threading.Tasks;

namespace DomainEvents
{
    /// <summary>
    /// Implement domain event publisher.
    /// </summary>
    public interface IPublisher
    {
        public Task RaiseAsync<T>(T @event) where T : IDomainEvent;
    }
}