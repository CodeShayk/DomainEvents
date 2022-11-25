namespace DomainEvents.Impl
{
    /// <summary>
    /// Default Resolver to return all handlers implemented for event type T.
    /// </summary>
    public sealed class Resolver : IResolver
    {
        private readonly IEnumerable<IHandle> _Handlers;

        public Resolver(IEnumerable<IHandle> handlers)
        {
            _Handlers = handlers;
        }

        public Task<IEnumerable<IHandle<T>>> ResolveAsync<T>() where T : IDomainEvent
        {
            var handlers = _Handlers.Where(t => typeof(IHandle<T>).IsAssignableFrom(t.GetType())).Cast<IHandle<T>>();
            return Task.FromResult(handlers);
        }
    }
}