using Application.Modules.Realtime;

namespace Api.Modules.Realtime;

public interface IRealtimeClient
{
    Task Receive(RealtimeMessage message);

    Task ReceiveResponse(RealtimeResponse response);
}
