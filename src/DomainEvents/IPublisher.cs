using System.Threading.Tasks;

namespace DomainEvents
{
    /// <summary>
    /// Implement domain event publisher.
    /// </summary>
    public interface IPublisher
    {
        Task RaiseAsync<T>(T @event) where T : IDomainEvent;
    }
}