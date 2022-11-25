namespace DomainEvents
{
    /// <summary>
    /// Implement Resolver to return all the handlers implemented for domain event type T
    /// </summary>
    public interface IResolver
    {
        Task<IEnumerable<IHandler<T>>> ResolveAsync<T>() where T : IDomainEvent;
    }
}