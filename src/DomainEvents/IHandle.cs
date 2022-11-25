namespace DomainEvents
{
    /// <summary>
    /// Interface to implement domain event handler.
    /// </summary>
    /// <typeparam name="T">Event Type</typeparam>
    public interface IHandler<T> : IHandler where T : IDomainEvent
    {
        Task HandleAsync(T @event);
    }

    public interface IHandler
    { }
}