using Cqrs.Abstractions;
using Domain.Modules.Files;
using Domain.Modules.Media;
using Persistence.Abstractions;
using Application.Modules.Files;

namespace Application.Modules.Media.Queries;

internal sealed class GetMediaAssetQueryHandler(IReadOnlyRepository repository) : IQueryHandler<GetMediaAssetQuery, MediaAssetModel?>
{
    public async Task<MediaAssetModel?> Handle(GetMediaAssetQuery request, CancellationToken cancellationToken)
    {
        var assetQuery = repository.Query(nameof(MediaAsset))
            .Where(nameof(MediaAsset.Id), request.Id)
            .WhereNull(nameof(MediaAsset.DeletedUtc));
        MediaAsset? asset = (await repository.GetAsync<MediaAsset>(assetQuery, cancellationToken)).FirstOrDefault();
        if (asset is null)
            return null;

        var fileQuery = repository.Query(nameof(StoredFile))
            .Where(nameof(StoredFile.Id), asset.StoredFileId)
            .WhereNull(nameof(StoredFile.DeletedUtc));
        StoredFile? file = (await repository.GetAsync<StoredFile>(fileQuery, cancellationToken)).FirstOrDefault();
        return file is null ? null : asset.ToModel(file.ToModel());
    }
}
