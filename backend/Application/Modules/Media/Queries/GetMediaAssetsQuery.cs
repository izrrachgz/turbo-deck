using Cqrs.Abstractions;

namespace Application.Modules.Media.Queries;

public sealed record GetMediaAssetsQuery(int Limit = 100) : IQuery<IReadOnlyList<MediaAssetModel>>;
