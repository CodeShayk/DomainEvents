using System;
using System.Diagnostics;

namespace DomainEvents
{
    /// <summary>
    /// OpenTelemetry activity source for DomainEvents.
    /// </summary>
    public static class DomainEventsActivitySource
    {
        /// <summary>
        /// Activity source name for DomainEvents.
        /// </summary>
        public const string ActivitySourceName = "DomainEvents";

        /// <summary>
        /// Activity source instance for DomainEvents.
        /// </summary>
        public static readonly ActivitySource Source = new ActivitySource(ActivitySourceName, "1.0.0");

        /// <summary>
        /// Activity name for event publishing.
        /// </summary>
        public const string PublishEventActivityName = "DomainEvents.Publish";

        /// <summary>
        /// Activity name for event handling.
        /// </summary>
        public const string HandleEventActivityName = "DomainEvents.Handle";
    }

    /// <summary>
    /// Activity tags for DomainEvents.
    /// </summary>
    public static class DomainEventsTags
    {
        /// <summary>
        /// Tag for event type.
        /// </summary>
        public const string EventType = "domain.event.type";

        /// <summary>
        /// Tag for handler type.
        /// </summary>
        public const string HandlerType = "domain.handler.type";

        /// <summary>
        /// Tag for aggregate type.
        /// </summary>
        public const string AggregateType = "domain.aggregate.type";

        /// <summary>
        /// Tag for error message.
        /// </summary>
        public const string ErrorMessage = "error.message";

        /// <summary>
        /// Tag for error type.
        /// </summary>
        public const string ErrorType = "error.type";
    }
}
