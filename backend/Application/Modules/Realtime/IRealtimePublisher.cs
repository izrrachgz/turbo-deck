namespace Application.Modules.Realtime;

public interface IRealtimePublisher
{
    Task PublishAsync(RealtimePublishRequest request, CancellationToken cancellationToken = default);
}
