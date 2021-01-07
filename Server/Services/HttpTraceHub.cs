using HttpTracer.Shared.Clients;
using Microsoft.AspNetCore.SignalR;

namespace HttpTracer.Server.Services
{
    public class HttpTraceHub : Hub<IHttpTraceClient>
    {
    }
}
