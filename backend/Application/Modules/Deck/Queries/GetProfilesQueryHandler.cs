using Cqrs.Abstractions;
using Domain.Modules.Deck;
using Persistence.Abstractions;

namespace Application.Modules.Deck.Queries;

internal sealed class GetProfilesQueryHandler(IReadOnlyRepository repository)
    : IQueryHandler<GetProfilesQuery, IReadOnlyList<ProfileModel>>
{
    public async Task<IReadOnlyList<ProfileModel>> Handle(GetProfilesQuery request, CancellationToken cancellationToken)
    {
        var query = repository.Query(nameof(Profile))
            .WhereNull(nameof(Profile.DeletedUtc))
            .OrderBy(nameof(Profile.SortOrder))
            .OrderBy(nameof(Profile.Id));
        IEnumerable<Profile> profiles = await repository.GetAsync<Profile>(query, cancellationToken);
        return profiles.Select(profile => profile.ToModel()).ToArray();
    }
}
