using System.Text.Json;
using Api.Modules.Media;
using Cqrs.Abstractions;
using System.IO.Compression;
using Application.Modules.Deck;
using Microsoft.AspNetCore.Mvc;
using Api.Modules.Deck.Requests;
using Api.Modules.Files.Policies;
using Application.Modules.Deck.Queries;
using Application.Modules.Deck.Commands;
using Application.Modules.Files.Queries;
using Application.Modules.Deck.Transfers;

namespace Api.Modules.Deck;

[ApiController]
[Route("api/v1")]
public sealed class DeckController(IRequestDispatcher dispatcher, IConfiguration configuration) : ControllerBase
{
    private const long MaximumPackageSizeBytes = 64L * 1024 * 1024 * 1024;
    private const int MaximumEntryCount = 1024;
    private const int MaximumManifestSizeBytes = 2 * 1024 * 1024;
    private static readonly JsonSerializerOptions TransferJsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly long _maximumMediaSizeBytes = configuration.GetValue<long>("Api:Files:MaximumFileSizeBytes");

    [HttpGet("profiles")]
    public async Task<ActionResult<IReadOnlyList<ProfileModel>>> GetProfiles(CancellationToken cancellationToken)
        => Ok(await dispatcher.Send(new GetProfilesQuery(), cancellationToken));

    [HttpPost("profiles")]
    public async Task<ActionResult<ProfileModel>> CreateProfile(CreateProfileRequest request, CancellationToken cancellationToken)
    {
        ProfileModel created = await dispatcher.Send(new CreateProfileCommand(request.Name), cancellationToken);
        return Created($"/api/v1/profiles/{created.Id}", created);
    }

    [HttpDelete("profiles/{profileId:long}")]
    public async Task<ActionResult<DeleteProfileResultModel>> DeleteProfile(
        long profileId,
        DeleteProfileRequest request,
        CancellationToken cancellationToken)
    {
        DeleteProfileResultModel? deleted = await dispatcher.Send(
            new DeleteProfileCommand(profileId, request.ConfirmationName),
            cancellationToken);
        return deleted is null ? NotFound() : Ok(deleted);
    }

    [HttpGet("profiles/{profileId:long}/export")]
    public async Task<IActionResult> ExportProfile(long profileId, CancellationToken cancellationToken)
    {
        ProfileExportModel? export = await dispatcher.Send(new GetProfileExportQuery(profileId), cancellationToken);
        if (export is null)
            return NotFound();

        Response.ContentType = "application/vnd.turbo-deck.profile+zip";
        Response.Headers.ContentDisposition = "attachment; filename=profile.turbo-deck";
        await using var archive = new ZipArchive(Response.Body, ZipArchiveMode.Create, leaveOpen: true);
        ZipArchiveEntry manifestEntry = archive.CreateEntry("manifest.json", CompressionLevel.Fastest);
        await using (Stream manifestStream = manifestEntry.Open())
            await JsonSerializer.SerializeAsync(manifestStream, export.Manifest, TransferJsonOptions, cancellationToken);

        foreach (ProfileExportFile exportFile in export.Files)
        {
            var stored = await dispatcher.Send(new OpenFileQuery(exportFile.StoredFileId), cancellationToken)
                ?? throw new FileNotFoundException("A media file referenced by the profile was not found.");
            await using Stream content = stored.Content;
            ZipArchiveEntry entry = archive.CreateEntry(exportFile.ArchivePath, CompressionLevel.NoCompression);
            await using Stream destination = entry.Open();
            await content.CopyToAsync(destination, cancellationToken);
        }
        return new EmptyResult();
    }

    [HttpPost("profiles/import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumPackageSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaximumPackageSizeBytes)]
    public async Task<ActionResult<ProfileImportResultModel>> ImportProfile(
        [FromForm] ImportProfileRequest request,
        CancellationToken cancellationToken)
    {
        IFormFile? file = request.File;
        if (file is null || file.Length is < 1 or > MaximumPackageSizeBytes ||
            !string.Equals(Path.GetExtension(file.FileName), ".turbo-deck", StringComparison.OrdinalIgnoreCase))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Select a valid Turbo Deck profile package.");

        await using Stream packageStream = file.OpenReadStream();
        using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read, leaveOpen: true);
        if (archive.Entries.Count is < 1 or > MaximumEntryCount)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "The profile package has an invalid number of entries.");
        if (archive.Entries.Select(entry => entry.FullName).Distinct(StringComparer.Ordinal).Count() != archive.Entries.Count)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "The profile package contains duplicate entries.");
        ZipArchiveEntry? manifestEntry = archive.GetEntry("manifest.json");
        if (manifestEntry is null || manifestEntry.Length is < 1 or > MaximumManifestSizeBytes)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "The profile package manifest is missing or invalid.");

        ProfileTransferManifest? manifest;
        await using (Stream manifestStream = manifestEntry.Open())
            manifest = await JsonSerializer.DeserializeAsync<ProfileTransferManifest>(manifestStream, TransferJsonOptions, cancellationToken);
        if (manifest is null || !HasRequiredManifestShape(manifest))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "The profile package manifest is invalid.");

        var referencedPaths = manifest.Media.Select(media => media.ArchivePath).ToHashSet(StringComparer.Ordinal);
        if (archive.Entries.Any(entry => entry.FullName != "manifest.json" && !referencedPaths.Contains(entry.FullName)) ||
            referencedPaths.Any(path => !IsSafeMediaPath(path) || archive.GetEntry(path) is null))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "The profile package contains unexpected or unsafe entries.");

        foreach (ProfileTransferMediaAsset media in manifest.Media)
        {
            ZipArchiveEntry entry = archive.GetEntry(media.ArchivePath)!;
            if (entry.Length != media.Size || !MediaUploadPolicy.IsAllowedTransfer(
                media.OriginalFileName,
                media.ContentType,
                entry.Length,
                _maximumMediaSizeBytes) ||
                !string.Equals(Path.GetExtension(media.OriginalFileName), media.Extension, StringComparison.OrdinalIgnoreCase) ||
                entry.Length > Math.Max(entry.CompressedLength * 20, entry.CompressedLength + 1024 * 1024))
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "The profile package contains invalid media.");
            await using Stream signatureStream = entry.Open();
            if (!await FileUploadPolicy.HasValidSignatureAsync(signatureStream, media.OriginalFileName, cancellationToken))
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "A packaged media file does not match its extension.");
        }

        var imports = manifest.Media.Select(media => new ProfileImportMedia(media.Key, archive.GetEntry(media.ArchivePath)!.Open())).ToArray();
        try
        {
            ProfileImportResultModel result = await dispatcher.Send(
                new ImportProfileCommand(manifest, imports, _maximumMediaSizeBytes),
                cancellationToken);
            return Created($"/api/v1/profiles/{result.Profile.Id}", result);
        }
        finally
        {
            foreach (ProfileImportMedia import in imports)
                await import.Content.DisposeAsync();
        }
    }

    [HttpGet("profiles/{profileId:long}/pages")]
    public async Task<ActionResult<IReadOnlyList<DeckPageModel>>> GetPages(long profileId, CancellationToken cancellationToken)
        => Ok(await dispatcher.Send(new GetProfilePagesQuery(profileId), cancellationToken));

    [HttpPost("profiles/{profileId:long}/pages")]
    public async Task<ActionResult<DeckPageModel>> CreatePage(long profileId, CancellationToken cancellationToken)
    {
        DeckPageModel created = await dispatcher.Send(new CreatePageCommand(profileId), cancellationToken);
        return Created($"/api/v1/pages/{created.Id}", created);
    }

    [HttpGet("pages/{pageId:long}/grid")]
    public async Task<ActionResult<DeckGridModel>> GetGrid(long pageId, CancellationToken cancellationToken)
    {
        DeckGridModel? grid = await dispatcher.Send(new GetDeckGridQuery(pageId), cancellationToken);
        return grid is null ? NotFound() : Ok(grid);
    }

    [HttpPut("pages/{pageId:long}/cells/{rowIndex:int}/{columnIndex:int}")]
    public async Task<ActionResult<CellConfigModel>> UpsertCell(
        long pageId,
        int rowIndex,
        int columnIndex,
        UpsertCellConfigRequest request,
        CancellationToken cancellationToken)
        => Ok(await dispatcher.Send(
            new UpsertCellConfigCommand(
                pageId,
                rowIndex,
                columnIndex,
                request.Label,
                request.Enabled,
                request.MediaAssetId,
                request.ThumbnailMediaAssetId,
                request.Volume,
                request.PlaybackRate,
                request.PitchSemitones,
                request.DisplayDurationMilliseconds,
                request.DisplayPermanently,
                request.DisplaySize,
                request.AnimationInType,
                request.AnimationInDurationMilliseconds,
                request.AnimationOutType,
                request.AnimationOutDurationMilliseconds,
                request.QueuePriority),
            cancellationToken));

    private static bool IsSafeMediaPath(string path) =>
        path.StartsWith("media/", StringComparison.Ordinal) &&
        !path.Contains('\\') &&
        !path.Contains("..", StringComparison.Ordinal) &&
        string.Equals(path, path.Replace("//", "/", StringComparison.Ordinal), StringComparison.Ordinal);

    private static bool HasRequiredManifestShape(ProfileTransferManifest manifest) =>
        manifest.Profile is not null &&
        manifest.Profile.Name is not null &&
        manifest.Profile.Pages is not null &&
        manifest.Profile.Pages.All(page => page is not null && page.Name is not null && page.Cells is not null &&
            page.Cells.All(cell => cell is not null && cell.Key is not null && cell.DisplaySize is not null &&
                cell.AnimationInType is not null && cell.AnimationOutType is not null && cell.QueuePriority is not null)) &&
        manifest.Media is not null &&
        manifest.Media.All(media => media is not null && media.Key is not null && media.ArchivePath is not null &&
            media.OriginalFileName is not null && media.ContentType is not null && media.Extension is not null &&
            media.Sha256 is not null && media.DisplayName is not null && media.Kind is not null && media.InspectionStatus is not null) &&
        manifest.TwitchRewardBindings is not null &&
        manifest.TwitchRewardBindings.All(binding => binding is not null && binding.RewardId is not null &&
            binding.RewardTitle is not null && binding.CellKey is not null);
}
