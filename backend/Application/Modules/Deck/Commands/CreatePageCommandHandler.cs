using Time.Extensions;
using Cqrs.Abstractions;
using SqlKata.Execution;
using Domain.Modules.Deck;
using System.Globalization;
using Persistence.Abstractions;
using Microsoft.Extensions.Logging;
using Application.Modules.Deck.Logging;

namespace Application.Modules.Deck.Commands;

internal sealed class CreatePageCommandHandler(
    IWritableRepository repository,
    TimeProvider timeProvider,
    ILogger<CreatePageCommandHandler> logger) : ICommandHandler<CreatePageCommand, DeckPageModel>
{
    public async Task<DeckPageModel> Handle(CreatePageCommand request, CancellationToken cancellationToken)
    {
        Profile? profile = await repository.Db.Query(nameof(Profile))
            .Where(nameof(Profile.Id), request.ProfileId)
            .WhereNull(nameof(Profile.DeletedUtc))
            .FirstOrDefaultAsync<Profile>(cancellationToken: cancellationToken);
        if (profile is null)
            throw new FileNotFoundException("The profile was not found.");

        IEnumerable<DeckPage> activePages = await repository.Db.Query(nameof(DeckPage))
            .Where(nameof(DeckPage.ProfileId), request.ProfileId)
            .WhereNull(nameof(DeckPage.DeletedUtc))
            .OrderBy(nameof(DeckPage.Position))
            .GetAsync<DeckPage>(cancellationToken: cancellationToken);
        DeckPage[] pages = activePages.ToArray();
        if (pages.Length >= 8)
            throw new InvalidDataException("A maximum of eight pages per profile is supported.");

        DateTime now = timeProvider.GetCurrentUtcDateTime();
        int position = pages.Length == 0 ? 1 : pages.Max(page => page.Position) + 1;
        string name = position.ToString(CultureInfo.InvariantCulture);
        long id = await repository.Db.Query(nameof(DeckPage)).InsertGetIdAsync<long>(new
        {
            request.ProfileId,
            Name = name,
            Position = position,
            CreatedUtc = now,
            ModifiedUtc = now,
            DeletedUtc = (DateTime?)null
        }, cancellationToken: cancellationToken);

        logger.PageCreated(id, request.ProfileId, position);
        return new DeckPage
        {
            Id = id,
            ProfileId = request.ProfileId,
            Name = name,
            Position = position,
            CreatedUtc = now,
            ModifiedUtc = now
        }.ToModel();
    }
}
