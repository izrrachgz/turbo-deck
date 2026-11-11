using Cqrs.Abstractions;
using Domain.Modules.Settings;
using Persistence.Abstractions;

namespace Application.Modules.Settings.Queries;

internal sealed class GetAppSettingsQueryHandler(IReadOnlyRepository repository) : IQueryHandler<GetAppSettingsQuery, AppSettingsModel>
{
    public async Task<AppSettingsModel> Handle(GetAppSettingsQuery request, CancellationToken cancellationToken)
    {
        var query = repository.Query(nameof(AppSettings))
            .Where(nameof(AppSettings.Id), 1)
            .WhereNull(nameof(AppSettings.DeletedUtc));
        AppSettings? settings = (await repository.GetAsync<AppSettings>(query, cancellationToken)).FirstOrDefault();
        return settings?.ToModel() ?? throw new InvalidDataException("Application settings are not initialized.");
    }
}
