namespace DomainEvents
{
    /// <summary>
    /// Implement Resolver to return all the handlers implemented for domain event type T
    /// </summary>
    public interface IResolver
    {
        Task<IEnumerable<IHandle<T>>> ResolveAsync<T>() where T : IDomainEvent;
    }
}