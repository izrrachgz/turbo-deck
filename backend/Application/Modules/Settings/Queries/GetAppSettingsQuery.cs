using Cqrs.Abstractions;

namespace Application.Modules.Settings.Queries;

public sealed record GetAppSettingsQuery : IQuery<AppSettingsModel>;
