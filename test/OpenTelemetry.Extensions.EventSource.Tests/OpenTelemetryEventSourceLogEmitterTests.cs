// <copyright file="OpenTelemetryEventSourceLogEmitterTests.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using Xunit;

namespace OpenTelemetry.Extensions.EventSource.Tests
{
    public class OpenTelemetryEventSourceLogEmitterTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void OpenTelemetryEventSourceLogEmitterDisposesProviderTests(bool dispose)
        {
            List<LogRecord> exportedItems = new();

#pragma warning disable CA2000 // Dispose objects before losing scope
            var openTelemetryLoggerProvider = new WrappedOpenTelemetryLoggerProvider(options =>
            {
                options.AddInMemoryExporter(exportedItems);
            });
#pragma warning restore CA2000 // Dispose objects before losing scope

            using (var openTelemetryEventSourceLogEmitter = new OpenTelemetryEventSourceLogEmitter(
                openTelemetryLoggerProvider,
                (name) => null,
                disposeProvider: dispose))
            {
            }

            Assert.Equal(dispose, openTelemetryLoggerProvider.Disposed);

            if (!dispose)
            {
                openTelemetryLoggerProvider.Dispose();
            }

            Assert.True(openTelemetryLoggerProvider.Disposed);
        }

        [Theory]
        [InlineData("OpenTelemetry.Extensions.EventSource.Tests", EventLevel.LogAlways, 2)]
        [InlineData("OpenTelemetry.Extensions.EventSource.Tests", EventLevel.Warning, 1)]
        [InlineData("_invalid_", EventLevel.LogAlways, 0)]
        public void OpenTelemetryEventSourceLogEmitterFilterTests(string sourceName, EventLevel? eventLevel, int expectedNumberOfLogRecords)
        {
            List<LogRecord> exportedItems = new();

#pragma warning disable CA2000 // Dispose objects before losing scope
            var openTelemetryLoggerProvider = new WrappedOpenTelemetryLoggerProvider(options =>
            {
                options.AddInMemoryExporter(exportedItems);
            });
#pragma warning restore CA2000 // Dispose objects before losing scope

            using (var openTelemetryEventSourceLogEmitter = new OpenTelemetryEventSourceLogEmitter(
                openTelemetryLoggerProvider,
                (name) => name == sourceName ? eventLevel : null))
            {
                TestEventSource.Log.SimpleEvent();
                TestEventSource.Log.ComplexEvent("Test_Message", 18);
            }

            Assert.Equal(expectedNumberOfLogRecords, exportedItems.Count);
        }

        [Fact]
        public void OpenTelemetryEventSourceLogEmitterCapturesExistingSourceTest()
        {
            List<LogRecord> exportedItems = new();

#pragma warning disable CA2000 // Dispose objects before losing scope
            var openTelemetryLoggerProvider = new WrappedOpenTelemetryLoggerProvider(options =>
            {
                options.AddInMemoryExporter(exportedItems);
            });
#pragma warning restore CA2000 // Dispose objects before losing scope

            TestEventSource.Log.SimpleEvent();

            using (var openTelemetryEventSourceLogEmitter = new OpenTelemetryEventSourceLogEmitter(
                openTelemetryLoggerProvider,
                (name) => name == "OpenTelemetry.Extensions.EventSource.Tests" ? EventLevel.LogAlways : null))
            {
                TestEventSource.Log.SimpleEvent();
            }

            Assert.Single(exportedItems);
        }

        [Fact]
        public void OpenTelemetryEventSourceLogEmitterSimpleEventTest()
        {
            List<LogRecord> exportedItems = new();

#pragma warning disable CA2000 // Dispose objects before losing scope
            var openTelemetryLoggerProvider = new WrappedOpenTelemetryLoggerProvider(options =>
            {
                options.AddInMemoryExporter(exportedItems);
            });
#pragma warning restore CA2000 // Dispose objects before losing scope

            using (var openTelemetryEventSourceLogEmitter = new OpenTelemetryEventSourceLogEmitter(
                openTelemetryLoggerProvider,
                (name) => name == "OpenTelemetry.Extensions.EventSource.Tests" ? EventLevel.LogAlways : null))
            {
                TestEventSource.Log.SimpleEvent();
            }

            Assert.Single(exportedItems);

            var logRecord = exportedItems[0];

            Assert.NotEqual(DateTime.MinValue, logRecord.Timestamp);
            Assert.Equal(TestEventSource.SimpleEventMessage, logRecord.FormattedMessage);
            Assert.Equal(TestEventSource.SimpleEventId, logRecord.EventId.Id);
            Assert.Equal(nameof(TestEventSource.SimpleEvent), logRecord.EventId.Name);
            Assert.Equal(LogLevel.Warning, logRecord.LogLevel);
            Assert.Null(logRecord.CategoryName);
            Assert.Null(logRecord.Exception);

            Assert.Equal(default, logRecord.TraceId);
            Assert.Equal(default, logRecord.SpanId);
            Assert.Null(logRecord.TraceState);
            Assert.Equal(ActivityTraceFlags.None, logRecord.TraceFlags);

            Assert.NotNull(logRecord.StateValues);
            Assert.Contains(logRecord.StateValues, kvp => kvp.Key == "event_source.name" && (string?)kvp.Value == "OpenTelemetry.Extensions.EventSource.Tests");
        }

        [Fact]
        public void OpenTelemetryEventSourceLogEmitterSimpleEventWithActivityTest()
        {
            using var activity = new Activity("Test");
            activity.Start();

            List<LogRecord> exportedItems = new();

#pragma warning disable CA2000 // Dispose objects before losing scope
            var openTelemetryLoggerProvider = new WrappedOpenTelemetryLoggerProvider(options =>
            {
                options.AddInMemoryExporter(exportedItems);
            });
#pragma warning restore CA2000 // Dispose objects before losing scope

            using (var openTelemetryEventSourceLogEmitter = new OpenTelemetryEventSourceLogEmitter(
                openTelemetryLoggerProvider,
                (name) => name == "OpenTelemetry.Extensions.EventSource.Tests" ? EventLevel.LogAlways : null))
            {
                TestEventSource.Log.SimpleEvent();
            }

            Assert.Single(exportedItems);

            var logRecord = exportedItems[0];

            Assert.NotEqual(default, logRecord.TraceId);

            Assert.Equal(activity.TraceId, logRecord.TraceId);
            Assert.Equal(activity.SpanId, logRecord.SpanId);
            Assert.Equal(activity.TraceStateString, logRecord.TraceState);
            Assert.Equal(activity.ActivityTraceFlags, logRecord.TraceFlags);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void OpenTelemetryEventSourceLogEmitterComplexEventTest(bool formatMessage)
        {
            List<LogRecord> exportedItems = new();

#pragma warning disable CA2000 // Dispose objects before losing scope
            var openTelemetryLoggerProvider = new WrappedOpenTelemetryLoggerProvider(options =>
            {
                options.IncludeFormattedMessage = formatMessage;
                options.AddInMemoryExporter(exportedItems);
            });
#pragma warning restore CA2000 // Dispose objects before losing scope

            using (var openTelemetryEventSourceLogEmitter = new OpenTelemetryEventSourceLogEmitter(
                openTelemetryLoggerProvider,
                (name) => name == "OpenTelemetry.Extensions.EventSource.Tests" ? EventLevel.LogAlways : null))
            {
                TestEventSource.Log.ComplexEvent("Test_Message", 18);
            }

            Assert.Single(exportedItems);

            var logRecord = exportedItems[0];

            Assert.NotEqual(DateTime.MinValue, logRecord.Timestamp);
            if (!formatMessage)
            {
                Assert.Equal(TestEventSource.ComplexEventMessageStructured, logRecord.FormattedMessage);
            }
            else
            {
                string expectedMessage = string.Format(CultureInfo.InvariantCulture, TestEventSource.ComplexEventMessage, "Test_Message", 18);
                Assert.Equal(expectedMessage, logRecord.FormattedMessage);
            }

            Assert.Equal(TestEventSource.ComplexEventId, logRecord.EventId.Id);
            Assert.Equal(nameof(TestEventSource.ComplexEvent), logRecord.EventId.Name);
            Assert.Equal(LogLevel.Information, logRecord.LogLevel);
            Assert.Null(logRecord.CategoryName);
            Assert.Null(logRecord.Exception);

            Assert.Equal(default, logRecord.TraceId);
            Assert.Equal(default, logRecord.SpanId);
            Assert.Null(logRecord.TraceState);
            Assert.Equal(ActivityTraceFlags.None, logRecord.TraceFlags);

            Assert.NotNull(logRecord.StateValues);
            Assert.Contains(logRecord.StateValues, kvp => kvp.Key == "event_source.name" && (string?)kvp.Value == "OpenTelemetry.Extensions.EventSource.Tests");
            Assert.Contains(logRecord.StateValues, kvp => kvp.Key == "arg1" && (string?)kvp.Value == "Test_Message");
            Assert.Contains(logRecord.StateValues, kvp => kvp.Key == "arg2" && (int?)kvp.Value == 18);
        }

        [Theory(Skip = "Not runnable in CI, see note.")]
        [InlineData(true)]
        [InlineData(false)]
        public void OpenTelemetryEventSourceLogEmitterActivityIdTest(bool enableTplListener)
        {
            /*
             * Note:
             *
             * To enable Activity ID the 'System.Threading.Tasks.TplEventSource'
             * source must be enabled see:
             * https://docs.microsoft.com/en-us/dotnet/core/diagnostics/eventsource-activity-ids#tracking-work-using-an-activity-id
             *
             * Once enabled, it cannot be turned off:
             * https://github.com/dotnet/runtime/blob/0fbdb1ed6e076829e4693a61ae5d11c4cb23e7ee/src/libraries/System.Private.CoreLib/src/System/Diagnostics/Tracing/ActivityTracker.cs#L208
             *
             * That behavior makes testing it difficult.
             */
            using var tplListener = enableTplListener ? new TplEventSourceListener() : null;

            List<LogRecord> exportedItems = new();

#pragma warning disable CA2000 // Dispose objects before losing scope
            var openTelemetryLoggerProvider = new WrappedOpenTelemetryLoggerProvider(options =>
            {
                options.AddInMemoryExporter(exportedItems);
            });
#pragma warning restore CA2000 // Dispose objects before losing scope

            using (var openTelemetryEventSourceLogEmitter = new OpenTelemetryEventSourceLogEmitter(
                openTelemetryLoggerProvider,
                (name) => name == "OpenTelemetry.Extensions.EventSource.Tests" ? EventLevel.LogAlways : null))
            {
                TestEventSource.Log.WorkStart();

                TestEventSource.Log.SubworkStart();

                TestEventSource.Log.SubworkStop();

                TestEventSource.Log.WorkStop();
            }

            Assert.Equal(4, exportedItems.Count);

            var logRecord = exportedItems[1];

            if (enableTplListener)
            {
                Assert.Contains(logRecord.StateValues, kvp => kvp.Key == "event_source.activity_id");
                Assert.Contains(logRecord.StateValues, kvp => kvp.Key == "event_source.related_activity_id");
            }
            else
            {
                Assert.DoesNotContain(logRecord.StateValues, kvp => kvp.Key == "event_source.activity_id");
                Assert.DoesNotContain(logRecord.StateValues, kvp => kvp.Key == "event_source.related_activity_id");
            }
        }

        private sealed class WrappedOpenTelemetryLoggerProvider : OpenTelemetryLoggerProvider
        {
            public WrappedOpenTelemetryLoggerProvider(Action<OpenTelemetryLoggerOptions> configure)
                : base(configure)
            {
            }

            public bool Disposed { get; private set; }

            protected override void Dispose(bool disposing)
            {
                this.Disposed = true;

                base.Dispose(disposing);
            }
        }

        [EventSource(Name = "OpenTelemetry.Extensions.EventSource.Tests")]
        private sealed class TestEventSource : System.Diagnostics.Tracing.EventSource
        {
            public const int SimpleEventId = 1;
            public const string SimpleEventMessage = "Warning event with no arguments.";

            public const int ComplexEventId = 2;
            public const string ComplexEventMessage = "Information event with two arguments: '{0}' & '{1}'.";
            public const string ComplexEventMessageStructured = "Information event with two arguments: '{arg1}' & '{arg2}'.";

            public static TestEventSource Log { get; } = new();

            [Event(SimpleEventId, Message = SimpleEventMessage, Level = EventLevel.Warning)]
            public void SimpleEvent()
            {
                this.WriteEvent(SimpleEventId);
            }

            [Event(ComplexEventId, Message = ComplexEventMessage, Level = EventLevel.Informational)]
            public void ComplexEvent(string arg1, int arg2)
            {
                this.WriteEvent(ComplexEventId, arg1, arg2);
            }

            [Event(3, Level = EventLevel.Verbose)]
            public void WorkStart()
            {
                this.WriteEvent(3);
            }

            [Event(4, Level = EventLevel.Verbose)]
            public void WorkStop()
            {
                this.WriteEvent(4);
            }

            [Event(5, Level = EventLevel.Verbose)]
            public void SubworkStart()
            {
                this.WriteEvent(5);
            }

            [Event(6, Level = EventLevel.Verbose)]
            public void SubworkStop()
            {
                this.WriteEvent(6);
            }
        }

        private sealed class TplEventSourceListener : EventListener
        {
            private readonly List<System.Diagnostics.Tracing.EventSource> eventSources = new();

            /// <inheritdoc/>
            public override void Dispose()
            {
                foreach (System.Diagnostics.Tracing.EventSource eventSource in this.eventSources)
                {
                    this.DisableEvents(eventSource);
                }

                this.eventSources.Clear();

                base.Dispose();
            }

            protected override void OnEventSourceCreated(System.Diagnostics.Tracing.EventSource eventSource)
            {
                if (eventSource.Name == "System.Threading.Tasks.TplEventSource")
                {
                    // Activity IDs aren't enabled by default.
                    // Enabling Keyword 0x80 on the TplEventSource turns them on
                    this.EnableEvents(eventSource, EventLevel.LogAlways, (EventKeywords)0x80);
                    this.eventSources.Add(eventSource);
                }
            }
        }
    }
}
