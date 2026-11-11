using Cqrs.Abstractions;
using Domain.Modules.Files;
using Domain.Modules.Media;
using Persistence.Abstractions;
using Application.Modules.Files;

namespace Application.Modules.Media.Queries;

internal sealed class GetMediaAssetsQueryHandler(IReadOnlyRepository repository)
    : IQueryHandler<GetMediaAssetsQuery, IReadOnlyList<MediaAssetModel>>
{
    public async Task<IReadOnlyList<MediaAssetModel>> Handle(GetMediaAssetsQuery request, CancellationToken cancellationToken)
    {
        var assetsQuery = repository.Query(nameof(MediaAsset))
            .WhereNull(nameof(MediaAsset.DeletedUtc))
            .OrderByDesc(nameof(MediaAsset.CreatedUtc))
            .Limit(request.Limit);
        MediaAsset[] assets = (await repository.GetAsync<MediaAsset>(assetsQuery, cancellationToken)).ToArray();
        if (assets.Length == 0)
            return [];

        var filesQuery = repository.Query(nameof(StoredFile))
            .WhereIn(nameof(StoredFile.Id), assets.Select(asset => asset.StoredFileId))
            .WhereNull(nameof(StoredFile.DeletedUtc));
        Dictionary<long, StoredFile> files = (await repository.GetAsync<StoredFile>(filesQuery, cancellationToken))
            .ToDictionary(file => file.Id);
        return assets
            .Where(asset => files.ContainsKey(asset.StoredFileId))
            .Select(asset => asset.ToModel(files[asset.StoredFileId].ToModel()))
            .ToArray();
    }
}
