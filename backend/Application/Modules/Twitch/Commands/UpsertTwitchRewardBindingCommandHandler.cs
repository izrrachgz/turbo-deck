using Time.Extensions;
using Cqrs.Abstractions;
using SqlKata.Execution;
using Domain.Modules.Deck;
using Domain.Modules.Twitch;
using Persistence.Abstractions;

namespace Application.Modules.Twitch.Commands;

internal sealed class UpsertTwitchRewardBindingCommandHandler(IWritableRepository repository, TimeProvider timeProvider)
    : ICommandHandler<UpsertTwitchRewardBindingCommand, TwitchRewardBindingModel>
{
    public async Task<TwitchRewardBindingModel> Handle(
        UpsertTwitchRewardBindingCommand request,
        CancellationToken cancellationToken)
    {
        string rewardId = request.RewardId.Trim();
        string rewardTitle = request.RewardTitle.Trim();
        if (rewardId.Length is < 1 or > 128 || rewardTitle.Length is < 1 or > 160)
            throw new InvalidDataException("The Twitch reward identifier or title is invalid.");

        CellConfig? cell = await repository.Db.Query(nameof(CellConfig))
            .Where(nameof(CellConfig.Id), request.CellConfigId)
            .WhereNull(nameof(CellConfig.DeletedUtc))
            .FirstOrDefaultAsync<CellConfig>(cancellationToken: cancellationToken);
        if (cell is null)
            throw new FileNotFoundException("The deck cell was not found.");

        TwitchRewardBinding? existing = await repository.Db.Query(nameof(TwitchRewardBinding))
            .Where(nameof(TwitchRewardBinding.RewardId), rewardId)
            .FirstOrDefaultAsync<TwitchRewardBinding>(cancellationToken: cancellationToken);
        DateTime now = timeProvider.GetCurrentUtcDateTime();

        if (existing is null)
        {
            long id = await repository.Db.Query(nameof(TwitchRewardBinding)).InsertGetIdAsync<long>(new
            {
                RewardId = rewardId,
                RewardTitle = rewardTitle,
                request.CellConfigId,
                request.Enabled,
                CreatedUtc = now,
                ModifiedUtc = now,
                DeletedUtc = (DateTime?)null
            }, cancellationToken: cancellationToken);
            existing = new TwitchRewardBinding
            {
                Id = id,
                RewardId = rewardId,
                RewardTitle = rewardTitle,
                CellConfigId = request.CellConfigId,
                Enabled = request.Enabled,
                CreatedUtc = now,
                ModifiedUtc = now
            };
        }
        else
        {
            await repository.Db.Query(nameof(TwitchRewardBinding))
                .Where(nameof(TwitchRewardBinding.Id), existing.Id)
                .UpdateAsync(new
                {
                    RewardTitle = rewardTitle,
                    request.CellConfigId,
                    request.Enabled,
                    ModifiedUtc = now,
                    DeletedUtc = (DateTime?)null
                }, cancellationToken: cancellationToken);
            existing.RewardTitle = rewardTitle;
            existing.CellConfigId = request.CellConfigId;
            existing.Enabled = request.Enabled;
            existing.ModifiedUtc = now;
            existing.DeletedUtc = null;
        }

        return existing.ToModel();
    }
}
