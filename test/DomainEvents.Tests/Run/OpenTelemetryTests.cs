using System.Diagnostics;
using DomainEvents;
using NUnit.Framework;

namespace DomainEvents.Tests.Run
{
    /// <summary>
    /// Tests for OpenTelemetry support.
    /// </summary>
    public class OpenTelemetryTests
    {
        [Test]
        public void DomainEventsActivitySource_ShouldHaveCorrectName()
        {
            // Assert
            Assert.That(DomainEventsActivitySource.ActivitySourceName, Is.EqualTo("DomainEvents"));
        }

        [Test]
        public void DomainEventsActivitySource_Source_ShouldNotBeNull()
        {
            // Assert
            Assert.That(DomainEventsActivitySource.Source, Is.Not.Null);
        }

        [Test]
        public void DomainEventsActivitySource_ShouldHaveCorrectVersion()
        {
            // Act
            var source = DomainEventsActivitySource.Source;

            // Assert
            Assert.That(source.Version, Is.EqualTo("1.0.0"));
        }

        [Test]
        public void DomainEventsTags_ShouldHaveCorrectEventType()
        {
            // Assert
            Assert.That(DomainEventsTags.EventType, Is.EqualTo("domain.event.type"));
        }

        [Test]
        public void DomainEventsTags_ShouldHaveCorrectHandlerType()
        {
            // Assert
            Assert.That(DomainEventsTags.HandlerType, Is.EqualTo("domain.handler.type"));
        }

        [Test]
        public void DomainEventsTags_ShouldHaveCorrectAggregateType()
        {
            // Assert
            Assert.That(DomainEventsTags.AggregateType, Is.EqualTo("domain.aggregate.type"));
        }

        [Test]
        public void DomainEventsTags_ShouldHaveCorrectErrorMessage()
        {
            // Assert
            Assert.That(DomainEventsTags.ErrorMessage, Is.EqualTo("error.message"));
        }

        [Test]
        public void DomainEventsTags_ShouldHaveCorrectErrorType()
        {
            // Assert
            Assert.That(DomainEventsTags.ErrorType, Is.EqualTo("error.type"));
        }

        [Test]
        public void ActivitySource_ShouldCreateActivity()
        {
            // Arrange
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == DomainEventsActivitySource.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
            };

            ActivitySource.AddActivityListener(listener);

            // Act
            using var activity = DomainEventsActivitySource.Source.StartActivity("TestActivity");

            // Assert
            Assert.That(activity, Is.Not.Null);
            Assert.That(activity.DisplayName, Is.EqualTo("TestActivity"));
        }

        [Test]
        public void Activity_ShouldSetTags()
        {
            // Arrange
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == DomainEventsActivitySource.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
            };

            ActivitySource.AddActivityListener(listener);

            // Act
            using var activity = DomainEventsActivitySource.Source.StartActivity("TestActivity");
            activity?.SetTag(DomainEventsTags.EventType, "TestEvent");
            activity?.SetTag(DomainEventsTags.HandlerType, "TestHandler");

            // Assert
            Assert.That(activity?.GetTagItem(DomainEventsTags.EventType), Is.EqualTo("TestEvent"));
            Assert.That(activity?.GetTagItem(DomainEventsTags.HandlerType), Is.EqualTo("TestHandler"));
        }

        [Test]
        public void Activity_ShouldSetStatus()
        {
            // Arrange
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == DomainEventsActivitySource.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
            };

            ActivitySource.AddActivityListener(listener);

            // Act
            using var activity = DomainEventsActivitySource.Source.StartActivity("TestActivity");
            activity?.SetStatus(ActivityStatusCode.Ok);

            // Assert
            Assert.That(activity?.Status, Is.EqualTo(ActivityStatusCode.Ok));
        }

        [Test]
        public void Activity_ShouldSetErrorStatus()
        {
            // Arrange
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == DomainEventsActivitySource.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
            };

            ActivitySource.AddActivityListener(listener);

            // Act
            using var activity = DomainEventsActivitySource.Source.StartActivity("TestActivity");
            activity?.SetStatus(ActivityStatusCode.Error, "Test error");

            // Assert
            Assert.That(activity?.Status, Is.EqualTo(ActivityStatusCode.Error));
            Assert.That(activity?.StatusDescription, Is.EqualTo("Test error"));
        }
    }
}
