using System.Data;
using Time.Extensions;
using Cqrs.Abstractions;
using SqlKata.Execution;
using System.Data.Common;
using Domain.Modules.Deck;
using Persistence.Abstractions;
using Microsoft.Extensions.Logging;
using Application.Modules.Deck.Logging;

namespace Application.Modules.Deck.Commands;

internal sealed class CreateProfileCommandHandler(
    IWritableRepository repository,
    TimeProvider timeProvider,
    ILogger<CreateProfileCommandHandler> logger) : ICommandHandler<CreateProfileCommand, ProfileModel>
{
    private const int PageCount = 8;

    public async Task<ProfileModel> Handle(CreateProfileCommand request, CancellationToken cancellationToken)
    {
        IEnumerable<Profile> activeProfiles = await repository.Db.Query(nameof(Profile))
            .WhereNull(nameof(Profile.DeletedUtc))
            .OrderBy(nameof(Profile.SortOrder))
            .GetAsync<Profile>(cancellationToken: cancellationToken);
        Profile[] profiles = activeProfiles.ToArray();
        if (profiles.Length >= 8)
            throw new InvalidDataException("A maximum of eight profiles is supported.");

        DateTime now = timeProvider.GetCurrentUtcDateTime();
        int sortOrder = profiles.Length == 0 ? 1 : profiles.Max(profile => profile.SortOrder) + 1;
        DbConnection connection = (DbConnection)repository.Db.Connection;
        bool closeConnection = connection.State != ConnectionState.Open;
        if (closeConnection)
            await connection.OpenAsync(cancellationToken);

        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        long profileId;
        var pageIds = new List<long>(PageCount);
        try
        {
            profileId = await repository.Db.Query(nameof(Profile)).InsertGetIdAsync<long>(new
            {
                Name = request.Name.Trim(),
                SortOrder = sortOrder,
                SelectedPageNumber = 1,
                CreatedUtc = now,
                ModifiedUtc = now,
                DeletedUtc = (DateTime?)null
            }, transaction: transaction, cancellationToken: cancellationToken);
            for (int position = 1; position <= PageCount; position++)
            {
                long pageId = await repository.Db.Query(nameof(DeckPage)).InsertGetIdAsync<long>(new
                {
                    ProfileId = profileId,
                    Name = position.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Position = position,
                    CreatedUtc = now,
                    ModifiedUtc = now,
                    DeletedUtc = (DateTime?)null
                }, transaction: transaction, cancellationToken: cancellationToken);
                pageIds.Add(pageId);
            }
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            if (closeConnection)
                await connection.CloseAsync();
        }

        logger.ProfileCreated(profileId, sortOrder);
        for (int index = 0; index < pageIds.Count; index++)
            logger.PageCreated(pageIds[index], profileId, index + 1);
        return new Profile
        {
            Id = profileId,
            Name = request.Name.Trim(),
            SortOrder = sortOrder,
            SelectedPageNumber = 1,
            CreatedUtc = now,
            ModifiedUtc = now
        }.ToModel();
    }
}
