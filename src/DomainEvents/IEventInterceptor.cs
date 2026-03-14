using Castle.DynamicProxy;

namespace DomainEvents
{
    /// <summary>
    /// Interface for custom event interceptors.
    /// Implement this interface to provide custom interception logic for aggregate event raising.
    /// </summary>
    public interface IEventInterceptor : IInterceptor
    {
    }
}
