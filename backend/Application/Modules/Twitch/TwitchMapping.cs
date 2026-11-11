using Domain.Modules.Twitch;

namespace Application.Modules.Twitch;

internal static class TwitchMapping
{
    public static TwitchRewardBindingModel ToModel(this TwitchRewardBinding binding) => new(
        binding.Id,
        binding.RewardId,
        binding.RewardTitle,
        binding.CellConfigId,
        binding.Enabled,
        binding.CreatedUtc,
        binding.ModifiedUtc);
}
