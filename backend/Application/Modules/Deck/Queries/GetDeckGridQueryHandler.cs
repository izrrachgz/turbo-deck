using Cqrs.Abstractions;
using Domain.Modules.Deck;
using Persistence.Abstractions;

namespace Application.Modules.Deck.Queries;

internal sealed class GetDeckGridQueryHandler(IReadOnlyRepository repository) : IQueryHandler<GetDeckGridQuery, DeckGridModel?>
{
    public async Task<DeckGridModel?> Handle(GetDeckGridQuery request, CancellationToken cancellationToken)
    {
        var pageQuery = repository.Query(nameof(DeckPage))
            .Where(nameof(DeckPage.Id), request.PageId)
            .WhereNull(nameof(DeckPage.DeletedUtc));
        DeckPage? page = (await repository.GetAsync<DeckPage>(pageQuery, cancellationToken)).FirstOrDefault();
        if (page is null)
            return null;

        var cellsQuery = repository.Query(nameof(CellConfig))
            .Where(nameof(CellConfig.PageId), request.PageId)
            .WhereNull(nameof(CellConfig.DeletedUtc))
            .OrderBy(nameof(CellConfig.RowIndex))
            .OrderBy(nameof(CellConfig.ColumnIndex));
        IEnumerable<CellConfig> cells = await repository.GetAsync<CellConfig>(cellsQuery, cancellationToken);
        return new DeckGridModel(page.ToModel(), cells.Select(cell => cell.ToModel()).ToArray());
    }
}
