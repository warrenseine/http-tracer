using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HttpTracer.Shared.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HttpTracer.Server.Services
{
    public sealed class HttpEventListenerService : EventListener, IHostedService
    {
        private const string EventSourceName = "Microsoft-System-Net-Http";

        private bool _started;
        private readonly CancellationToken _cancellationToken;
        private readonly ILogger<HttpEventListenerService> _logger;

        public HttpEventListenerService(IHostApplicationLifetime applicationLifetime, ILogger<HttpEventListenerService> logger)
        {
            _cancellationToken = applicationLifetime.ApplicationStopping;
            _logger = logger;
        }

        public int GetHttpTraceEventCount()
        {
            return HttpTraceEvent.GetEventCount();
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            base.OnEventSourceCreated(eventSource);

            if (eventSource.Name == EventSourceName)
            {
                EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            base.OnEventWritten(eventData);

            if (_cancellationToken.IsCancellationRequested || !_started || eventData.Payload == null)
            {
                return;
            }

            try
            {
                var httpTraceEvent = eventData.EventId switch
                {
                    (int) HttpTraceEvent.EventType.HandlerMessage => ReadHandlerMessageEvent(eventData),
                    (int) HttpTraceEvent.EventType.Error => ReadErrorEvent(eventData),
                    _ => null
                };

                if (httpTraceEvent != null)
                {
                    Console.WriteLine(httpTraceEvent);
                }
                else
                {
                    Console.WriteLine($"Skipped an event: {eventData.EventName} ({eventData.EventId}) {string.Join(",", eventData.Payload)}");
                }
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, e.Message);
            }
        }

        private static readonly IReadOnlyList<string> IgnoredHandlerMessageMethods = new[]
        {
            ".ctor", "IncrementConnectionCountNoLock", "DecrementConnectionCount", "GetHttpConnectionAsync",
            "TraceConnection", "ReturnConnection", "WriteToStreamAsync", "FillAsync",
            "ReadBufferedAsyncCore", "CopyFromBufferAsync", "CleanCacheAndDisposeIfUnused", "IsUsable", "Dispose"
        };

        private HttpTraceEvent ReadHandlerMessageEvent(EventWrittenEventArgs eventData)
        {
            if (eventData?.Payload?.Count != 5)
            {
                throw new InvalidOperationException($"Unexpected payload length in {nameof(ReadHandlerMessageEvent)}");
            }

            var methodName = (string) eventData.Payload[3];

            if (IgnoredHandlerMessageMethods.Contains(methodName))
            {
                // Accept, but ignore.
                return null;
            }

            return new HttpTraceEvent(
                type: HttpTraceEvent.EventType.HandlerMessage,
                requestId: (int) (eventData.Payload[2] ?? 0),
                methodName: methodName,
                message: (string) eventData.Payload[4]
            );
        }

        private HttpTraceEvent ReadErrorEvent(EventWrittenEventArgs eventData)
        {
            if (eventData?.Payload?.Count != 3)
            {
                throw new InvalidOperationException($"Unexpected payload length in {nameof(ReadErrorEvent)}");
            }

            return new HttpTraceEvent(
                type: HttpTraceEvent.EventType.Error,
                reference: (string) eventData.Payload[0],
                methodName: (string) eventData.Payload[1],
                message: (string) eventData.Payload[2]
            );
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _started = true;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _started = false;
            return Task.CompletedTask;
        }
    }
}
