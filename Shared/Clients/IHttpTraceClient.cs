using System.Threading.Tasks;
using HttpTracer.Shared.Events;

namespace HttpTracer.Shared.Clients
{
    public interface IHttpTraceClient
    {
        Task PushHttpTrace(HttpTraceEvent messageEvent);
    }
}
