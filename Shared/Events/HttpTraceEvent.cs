using System.Threading;

namespace HttpTracer.Shared.Events
{
    public class HttpTraceEvent
    {
        public enum EventType
        {
            Unknown = 0,
            Enter = 1,
            Exit = 2,
            Associate = 3,
            Info = 4,
            Error = 5,
            UriBaseAddress = 17,
            ClientSendCompleted = 19,
            HandlerMessage = 21,
        }

        public int SequenceId { get; set; }
        public EventType Type { get; set; }
        public string Reference { get; set; } // e.g. HttpClient#123456
        public int RequestId { get; set; }
        public string MethodName { get; set; }
        public string Message { get; set; }

        private static int _counter = 0;

        public HttpTraceEvent(EventType type, string reference = null, int requestId = 0, string methodName = null, string message = null)
        {
            SequenceId = Interlocked.Increment(ref _counter);
            Type = type;
            Reference = reference;
            RequestId = requestId;
            MethodName = methodName;
            Message = message;
        }

        public override string ToString()
        {
            return $"{SequenceId}, {Type}, {Reference}, {RequestId}, {MethodName}, {Message}";
        }

        public static int GetEventCount()
        {
            return _counter;
        }
    }
}
