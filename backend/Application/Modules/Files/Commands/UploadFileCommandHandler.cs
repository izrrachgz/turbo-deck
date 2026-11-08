using Cqrs.Abstractions;
using Domain.Modules.Files;
using Persistence.Abstractions;
using SqlKata.Execution;
using Time.Extensions;

namespace Application.Modules.Files.Commands;

internal sealed class UploadFileCommandHandler(
    IWritableRepository repository,
    IFileStorage storage,
    TimeProvider timeProvider) : ICommandHandler<UploadFileCommand, StoredFileModel>
{
    public async Task<StoredFileModel> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        string fileName = Path.GetFileName(request.FileName.Trim());
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        FileStorageWriteResult written = await storage.SaveAsync(
            request.Content,
            extension,
            request.MaximumSizeBytes,
            cancellationToken);

        DateTime now = timeProvider.GetCurrentUtcDateTime();
        try
        {
            long id = await repository.Db.Query(nameof(StoredFile)).InsertGetIdAsync<long>(new
            {
                OriginalFileName = fileName,
                written.StorageName,
                request.ContentType,
                Extension = extension,
                written.Size,
                written.Sha256,
                request.Purpose,
                CreatedUtc = now,
                ModifiedUtc = now,
                DeletedUtc = (DateTime?)null
            }, cancellationToken: cancellationToken);

            return new StoredFile
            {
                Id = id,
                OriginalFileName = fileName,
                StorageName = written.StorageName,
                ContentType = request.ContentType,
                Extension = extension,
                Size = written.Size,
                Sha256 = written.Sha256,
                Purpose = request.Purpose,
                CreatedUtc = now,
                ModifiedUtc = now
            }.ToModel();
        }
        catch
        {
            try
            {
                await storage.DeleteAsync(written.StorageName, CancellationToken.None);
            }
            catch (IOException)
            {
                // Preserve the database exception; startup maintenance can remove an orphaned file.
            }
            catch (UnauthorizedAccessException)
            {
                // Preserve the database exception; startup maintenance can remove an orphaned file.
            }
            throw;
        }
    }
}
