using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DomainEvents
{
    /// <summary>
    /// Listener that processes events from the queue via subscription.
    /// </summary>
    public interface IEventListener
    {
        /// <summary>
        /// Starts listening to the event queue.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops listening to the event queue.
        /// </summary>
        Task StopAsync();
    }
}
