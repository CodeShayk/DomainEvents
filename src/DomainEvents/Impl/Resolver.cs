namespace DomainEvents.Impl
{
    /// <summary>
    /// Default Resolver to return all handlers implemented for event type T.
    /// </summary>
    public sealed class Resolver : IResolver
    {
        private readonly IEnumerable<IHandler> _Handlers;

        public Resolver(IEnumerable<IHandler> handlers)
        {
            _Handlers = handlers;
        }

        public Task<IEnumerable<IHandler<T>>> ResolveAsync<T>() where T : IDomainEvent
        {
            var handlers = _Handlers.Where(t => typeof(IHandler<T>).IsAssignableFrom(t.GetType())).Cast<IHandler<T>>();
            return Task.FromResult(handlers);
        }
    }
}