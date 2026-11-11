using Cqrs.Abstractions;
using Domain.Modules.Twitch;
using Persistence.Abstractions;

namespace Application.Modules.Twitch.Queries;

internal sealed class GetTwitchRewardBindingsQueryHandler(IReadOnlyRepository repository)
    : IQueryHandler<GetTwitchRewardBindingsQuery, IReadOnlyList<TwitchRewardBindingModel>>
{
    public async Task<IReadOnlyList<TwitchRewardBindingModel>> Handle(
        GetTwitchRewardBindingsQuery request,
        CancellationToken cancellationToken)
    {
        var query = repository.Query(nameof(TwitchRewardBinding))
            .WhereNull(nameof(TwitchRewardBinding.DeletedUtc))
            .OrderBy(nameof(TwitchRewardBinding.RewardTitle));
        IEnumerable<TwitchRewardBinding> bindings = await repository.GetAsync<TwitchRewardBinding>(query, cancellationToken);
        return bindings.Select(binding => binding.ToModel()).ToArray();
    }
}
