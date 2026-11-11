using Time.Extensions;
using Cqrs.Abstractions;
using SqlKata.Execution;
using Domain.Modules.Deck;
using Domain.Modules.Settings;
using Persistence.Abstractions;
using Microsoft.Extensions.Logging;
using Application.Modules.Settings.Logging;

namespace Application.Modules.Settings.Commands;

internal sealed class UpdateAppSettingsCommandHandler(
    IWritableRepository repository,
    TimeProvider timeProvider,
    ILogger<UpdateAppSettingsCommandHandler> logger) : ICommandHandler<UpdateAppSettingsCommand, AppSettingsModel>
{
    public async Task<AppSettingsModel> Handle(UpdateAppSettingsCommand request, CancellationToken cancellationToken)
    {
        if (request.ActiveProfileId is long activeProfileId)
        {
            Profile? profile = await repository.Db.Query(nameof(Profile))
                .Where(nameof(Profile.Id), activeProfileId)
                .WhereNull(nameof(Profile.DeletedUtc))
                .FirstOrDefaultAsync<Profile>(cancellationToken: cancellationToken);
            if (profile is null)
                throw new FileNotFoundException("The active profile was not found.");
        }

        DateTime now = timeProvider.GetCurrentUtcDateTime();
        int updated = await repository.Db.Query(nameof(AppSettings))
            .Where(nameof(AppSettings.Id), 1)
            .WhereNull(nameof(AppSettings.DeletedUtc))
            .UpdateAsync(new
            {
                request.ActiveProfileId,
                request.Culture,
                request.Theme,
                request.Palette,
                TimeZone = request.TimeZone.Trim(),
                request.QueueCapacity,
                request.VisualizerSafeMargin,
                ModifiedUtc = now
            }, cancellationToken: cancellationToken);
        if (updated == 0)
            throw new InvalidDataException("Application settings are not initialized.");

        AppSettings settings = await repository.Db.Query(nameof(AppSettings))
            .Where(nameof(AppSettings.Id), 1)
            .FirstAsync<AppSettings>(cancellationToken: cancellationToken);
        logger.AppSettingsUpdated(request.Culture, request.Theme, request.TimeZone);
        return settings.ToModel();
    }
}
