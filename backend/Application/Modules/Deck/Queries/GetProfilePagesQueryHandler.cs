using Cqrs.Abstractions;
using Domain.Modules.Deck;
using Persistence.Abstractions;

namespace Application.Modules.Deck.Queries;

internal sealed class GetProfilePagesQueryHandler(IReadOnlyRepository repository)
    : IQueryHandler<GetProfilePagesQuery, IReadOnlyList<DeckPageModel>>
{
    public async Task<IReadOnlyList<DeckPageModel>> Handle(GetProfilePagesQuery request, CancellationToken cancellationToken)
    {
        var query = repository.Query(nameof(DeckPage))
            .Where(nameof(DeckPage.ProfileId), request.ProfileId)
            .WhereNull(nameof(DeckPage.DeletedUtc))
            .OrderBy(nameof(DeckPage.Position));
        IEnumerable<DeckPage> pages = await repository.GetAsync<DeckPage>(query, cancellationToken);
        return pages.Select(page => page.ToModel()).ToArray();
    }
}
