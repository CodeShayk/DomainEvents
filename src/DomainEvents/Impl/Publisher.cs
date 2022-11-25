namespace DomainEvents.Impl
{
    /// <summary>
    /// Publisher for domain event.
    /// </summary>
    public sealed class Publisher : IPublisher
    {
        private readonly IResolver _Resolver;

        public Publisher(IResolver resolver)
        {
            _Resolver = resolver;
        }

        public async Task RaiseAsync<T>(T @event) where T : IDomainEvent
        {
            var handlers = await _Resolver.ResolveAsync<T>();
            foreach (var handler in handlers.ToArray())
                await handler.HandleAsync(@event);
        }
    }
}