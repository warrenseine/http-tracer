using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HttpTracer.Shared.Events;

namespace HttpTracer.Shared.Models
{
    public class RequestStore
    {
        public IDictionary<int, RequestState> RequestsById { get; }
        public IList<RequestState> Requests { get; }
        public IList<string> Errors { get; }

        private static readonly Regex SendAsyncCoreSendingRegex = new Regex(
            @"^Sending request: Method: (?<method>\w+), RequestUri: '(?<url>.+?)', Version: \d\.\d, Content: \S+?, Headers:\s*{\s*(?<headers>[^}]*?)}$",
            RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex SendAsyncCoreReceivingRegex = new Regex(
            @"^Received response: StatusCode: (?<status>\d+), ReasonPhrase: '(?<reason>[\w\s]+)', Version: \d\.\d, Content: \S+?, Headers:\s*{\s*(?<headers>[^}]*?)}$",
            RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex HeadersRegex = new Regex(@"(\s*(?<key>[^:]+)\s*:\s*(?<value>.+)\s*)*",
            RegexOptions.Compiled | RegexOptions.Multiline);

        public RequestStore()
        {
            RequestsById = new Dictionary<int, RequestState>();
            Requests = new List<RequestState>();
            Errors = new List<string>();
        }

        private readonly object _syncLock = new object();
        public bool ReceiveEvent(HttpTraceEvent messageEvent)
        {
            lock (_syncLock)
            {
                return messageEvent.MethodName switch
                {
                    "SendAsyncCore" => OnSendAsyncCore(messageEvent),
                    "HandleFinishSendAsyncError" => OnErrorHttpClientSendAsync(messageEvent),
                    _ => false
                };
            }
        }

        private bool OnSendAsyncCore(HttpTraceEvent messageEvent)
        {
            var match = SendAsyncCoreSendingRegex.Match(messageEvent.Message);

            if (match.Success)
            {
                if (RequestsById.TryGetValue(messageEvent.RequestId, out _))
                {
                    throw new InvalidOperationException(
                        $"Unexpected SendAsyncCore with a used request ID: {messageEvent.RequestId}");
                }

                var url = match.Groups["url"].Value;
                var headers = ParseHeaders(match.Groups["headers"].Value);

                var requestState = new RequestState(messageEvent.RequestId, url, headers);

                RequestsById[messageEvent.RequestId] = requestState;
                Requests.Add(requestState);

                return true;
            }

            match = SendAsyncCoreReceivingRegex.Match(messageEvent.Message);

            if (match.Success)
            {
                if (!RequestsById.TryGetValue(messageEvent.RequestId, out var requestState))
                {
                    throw new InvalidOperationException(
                        $"Unexpected SendAsyncCore with an unknown request ID: {messageEvent.RequestId}");
                }

                var statusCode = int.Parse(match.Groups["status"].Value);
                var headers = ParseHeaders(match.Groups["headers"].Value);

                requestState.Complete(statusCode, headers);

                return true;
            }

            return false;
        }

        private bool OnErrorHttpClientSendAsync(HttpTraceEvent httpTraceEvent)
        {
            Errors.Add(httpTraceEvent.Message);
            return true;
        }

        private static IList<(string, string)> ParseHeaders(string rawHeaders)
        {
            var match = HeadersRegex.Match(rawHeaders);

            if (!match.Success)
            {
                throw new InvalidOperationException($"Unexpected header format: {rawHeaders}");
            }

            return match.Groups["key"].Captures.Zip(match.Groups["value"].Captures,
                (first, second) => (first.Value, second.Value)).ToList();
        }
    }
}
