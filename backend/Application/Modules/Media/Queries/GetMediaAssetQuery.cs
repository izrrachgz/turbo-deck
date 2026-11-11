using Cqrs.Abstractions;

namespace Application.Modules.Media.Queries;

public sealed record GetMediaAssetQuery(long Id) : IQuery<MediaAssetModel?>;
