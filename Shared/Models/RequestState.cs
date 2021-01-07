using System;
using System.Collections.Generic;
using System.Linq;

namespace HttpTracer.Shared.Models
{
    public class RequestState
    {
        public int RequestId { get; }
        public string Url { get; }
        public int StatusCode { get; private set; }
        public DateTimeOffset StartTime { get; }
        public DateTimeOffset? EndTime { get; private set; }
        public IList<(string, string)> RequestHeaders { get; }
        public IList<(string, string)> ResponseHeaders { get; private set; }

        private static readonly IList<(string, string)> EmptyHeaders = new List<(string, string)>();

        public RequestState(int requestId, string url, IList<(string, string)> requestHeaders)
        {
            RequestId = requestId;
            Url = url;
            RequestHeaders = requestHeaders;
            ResponseHeaders = EmptyHeaders;
            StartTime = DateTimeOffset.UtcNow;
        }

        public void Complete(int statusCode, IList<(string, string)> responseHeaders)
        {
            StatusCode = statusCode;
            ResponseHeaders = responseHeaders;
            EndTime = DateTimeOffset.UtcNow;
        }

        public override string ToString()
        {
            return $"{RequestId}, {Url}, {StatusCode}";
        }

        public string Status => StatusCode > 0 ? StatusCode.ToString() : "(pending)";
        public string Name => Url.Split("/").Last();
        public string ContentType => ResponseHeaders.FirstOrDefault(kv => kv.Item1 == "Content-Type").Item2;

        public string ContentLength =>
            int.TryParse(ResponseHeaders.FirstOrDefault(kv => kv.Item1 == "Content-Length").Item2, out var bytes)
                ? HumanizeBytes(bytes)
                : string.Empty;

        public string Time => IsComplete ? HumanizeDuration(EndTime!.Value.Subtract(StartTime)) : string.Empty;

        public bool IsComplete => EndTime.HasValue;

        private static string HumanizeDuration(TimeSpan timeSpan)
        {
            if (timeSpan.Milliseconds < 1000)
            {
                return $"{timeSpan.Milliseconds} ms";
            }

            if (timeSpan.Seconds < 60)
            {
                return $"{timeSpan.Seconds} s";
            }

            return $"{timeSpan.Minutes} min";
        }

        private static string HumanizeBytes(int bytes)
        {
            if (bytes < 1_000)
            {
                return $"{bytes} B";
            }

            if (bytes < 1_000_000)
            {
                return $"{bytes / 1_000} kB";
            }

            return $"{bytes / 1_000_000} MB";
        }
    }
}
