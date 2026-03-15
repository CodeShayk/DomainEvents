using System.Threading.Tasks;

namespace DomainEvents
{
    /// <summary>
    /// Interface for domain aggregates that can publish domain events.
    /// Extends IPublisher to provide event publishing capabilities.
    /// </summary>
    public interface IDomainAggregate : IPublisher
    {
    }
}
