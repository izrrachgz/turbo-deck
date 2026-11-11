using Cqrs.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Application.Modules.Files;
using Application.Modules.Media;
using Localization.Abstractions;
using Api.Modules.Files.Policies;
using Api.Modules.Media.Requests;
using Application.Modules.Files.Queries;
using Application.Modules.Media.Queries;
using Application.Localization.Resources;
using Application.Modules.Media.Commands;

namespace Api.Modules.Media;

[ApiController]
[Route("api/v1/media-assets")]
public sealed class MediaAssetsController(
    IRequestDispatcher dispatcher,
    IConfiguration configuration,
    ITextProvider messages) : ControllerBase
{
    private readonly long _maximumFileSizeBytes = configuration.GetValue<long>("Api:Files:MaximumFileSizeBytes");

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<MediaAssetModel>> Upload(
        [FromForm] UploadMediaAssetRequest request,
        CancellationToken cancellationToken)
    {
        FileUploadValidationFailure failure = MediaUploadPolicy.Validate(request.File, _maximumFileSizeBytes);
        if (failure != FileUploadValidationFailure.None)
        {
            string message = failure switch
            {
                FileUploadValidationFailure.FileRequired => messages.Get(nameof(Messages.File_Is_Required)),
                FileUploadValidationFailure.FileTooLarge => messages.Get(nameof(Messages.File_Exceeds_Maximum_Size), _maximumFileSizeBytes),
                _ => messages.Get(nameof(Messages.File_Type_Not_Allowed))
            };
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: message);
        }

        if (!await FileUploadPolicy.HasValidSignatureAsync(request.File!, cancellationToken))
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: messages.Get(nameof(Messages.File_Content_Does_Not_Match_Extension)));

        IFormFile file = request.File!;
        await using Stream content = file.OpenReadStream();
        MediaAssetModel created = await dispatcher.Send(new UploadMediaAssetCommand(
            file.FileName,
            file.ContentType,
            content,
            _maximumFileSizeBytes,
            request.DisplayName), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MediaAssetModel>>> List(
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default) =>
        Ok(await dispatcher.Send(new GetMediaAssetsQuery(limit), cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<MediaAssetModel>> Get(long id, CancellationToken cancellationToken)
    {
        MediaAssetModel? asset = await dispatcher.Send(new GetMediaAssetQuery(id), cancellationToken);
        return asset is null ? NotFound() : Ok(asset);
    }

    [HttpGet("{id:long}/content")]
    public async Task<IActionResult> Content(long id, CancellationToken cancellationToken)
    {
        MediaAssetModel? asset = await dispatcher.Send(new GetMediaAssetQuery(id), cancellationToken);
        if (asset is null)
            return NotFound();
        StoredFileContent? file = await dispatcher.Send(new OpenFileQuery(asset.File.Id), cancellationToken);
        return file is null ? NotFound() : File(file.Content, file.File.ContentType, enableRangeProcessing: true);
    }
}
