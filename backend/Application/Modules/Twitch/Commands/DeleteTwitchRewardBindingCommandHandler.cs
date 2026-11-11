using Time.Extensions;
using Cqrs.Abstractions;
using SqlKata.Execution;
using Domain.Modules.Twitch;
using Persistence.Abstractions;

namespace Application.Modules.Twitch.Commands;

internal sealed class DeleteTwitchRewardBindingCommandHandler(IWritableRepository repository, TimeProvider timeProvider)
    : ICommandHandler<DeleteTwitchRewardBindingCommand, bool>
{
    public async Task<bool> Handle(DeleteTwitchRewardBindingCommand request, CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetCurrentUtcDateTime();
        int updated = await repository.Db.Query(nameof(TwitchRewardBinding))
            .Where(nameof(TwitchRewardBinding.RewardId), request.RewardId.Trim())
            .WhereNull(nameof(TwitchRewardBinding.DeletedUtc))
            .UpdateAsync(new { DeletedUtc = now, ModifiedUtc = now }, cancellationToken: cancellationToken);
        return updated != 0;
    }
}
