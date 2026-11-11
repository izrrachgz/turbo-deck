using Cqrs.Abstractions;
using Domain.Modules.Twitch;
using Persistence.Abstractions;
using Application.Modules.Playback;
using Application.Modules.Playback.Commands;

namespace Application.Modules.Twitch.Commands;

internal sealed class TriggerTwitchRewardCommandHandler(IReadOnlyRepository repository, IRequestDispatcher dispatcher)
    : ICommandHandler<TriggerTwitchRewardCommand, PlaybackQueueSnapshotModel?>
{
    public async Task<PlaybackQueueSnapshotModel?> Handle(TriggerTwitchRewardCommand request, CancellationToken cancellationToken)
    {
        var query = repository.Query(nameof(TwitchRewardBinding))
            .Where(nameof(TwitchRewardBinding.RewardId), request.RewardId)
            .Where(nameof(TwitchRewardBinding.Enabled), true)
            .WhereNull(nameof(TwitchRewardBinding.DeletedUtc));
        TwitchRewardBinding? binding = (await repository.GetAsync<TwitchRewardBinding>(query, cancellationToken)).FirstOrDefault();
        return binding is null
            ? null
            : await dispatcher.Send(new QueueCellCommand(binding.CellConfigId), cancellationToken);
    }
}
