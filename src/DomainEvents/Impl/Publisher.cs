using System.Linq;
using System.Threading.Tasks;

namespace DomainEvents.Impl
{
    /// <summary>
    /// Publisher for domain event.
    /// </summary>
    public sealed class Publisher : IPublisher
    {
        private readonly IResolver _resolver;

        public Publisher(IResolver resolver)
        {
            _resolver = resolver;
        }

        public async Task RaiseAsync<T>(T @event) where T : IDomainEvent
        {
            var handlers = await _resolver.ResolveAsync<T>();
            foreach (var handler in handlers.ToArray())
                await handler.HandleAsync(@event);
        }
    }
}