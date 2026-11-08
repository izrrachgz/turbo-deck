using Microsoft.Extensions.Options;
using Application.Infrastructure.Modules.Files;
using Microsoft.Extensions.Logging.Abstractions;

namespace Api.Test.Modules.Files;

public sealed class LocalFileStorageTest
{
    [Fact]
    public async Task Storage_rejects_paths_and_cleans_up_deleted_content()
    {
        string root = Path.Combine(Path.GetTempPath(), $"command-q-storage-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var storage = new LocalFileStorage(Options.Create(new FileStorageOptions
            {
                DataDirectory = root,
                RootPath = "files"
            }), NullLogger<LocalFileStorage>.Instance);
            await using var content = new MemoryStream("stored content"u8.ToArray());

            var result = await storage.SaveAsync(content, ".txt", 1024, TestContext.Current.CancellationToken);
            Stream? escaped = await storage.OpenReadAsync("../outside.txt", TestContext.Current.CancellationToken);

            Assert.Null(escaped);
            Assert.Equal(64, result.Sha256.Length);

            await using (Stream? opened = await storage.OpenReadAsync(result.StorageName, TestContext.Current.CancellationToken))
                Assert.NotNull(opened);

            await storage.DeleteAsync(result.StorageName, TestContext.Current.CancellationToken);
            Assert.Null(await storage.OpenReadAsync(result.StorageName, TestContext.Current.CancellationToken));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
