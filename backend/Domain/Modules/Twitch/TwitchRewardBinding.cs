using Domain.Entities;

namespace Domain.Modules.Twitch;

public sealed class TwitchRewardBinding : BaseEntity
{
    public required string RewardId { get; set; }

    public required string RewardTitle { get; set; }

    public long CellConfigId { get; set; }

    public bool Enabled { get; set; } = true;
}
