using Api.Configuration;
using Api.Modules.Files.Policies;
using Api.Modules.Files.Requests;
using Application.Modules.Files;
using Application.Modules.Files.Commands;
using Application.Modules.Files.Queries;
using Cqrs.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Files;

[ApiController]
[Route("files")]
public sealed class FilesController(IRequestDispatcher dispatcher, IConfiguration configuration) : ControllerBase
{
    private readonly long _maximumFileSizeBytes = configuration.GetValue<long>("Api:Files:MaximumFileSizeBytes");

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<StoredFileModel>> Upload(
        [FromForm] UploadFileRequest request,
        CancellationToken cancellationToken)
    {
        FileUploadValidationFailure failure = FileUploadPolicy.Validate(request.File, _maximumFileSizeBytes);
        if (failure != FileUploadValidationFailure.None)
        {
            string message = failure switch
            {
                FileUploadValidationFailure.FileRequired => "A non-empty file is required.",
                FileUploadValidationFailure.FileTooLarge => $"The file exceeds {_maximumFileSizeBytes} bytes.",
                _ => "The file extension and content type are not allowed."
            };
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: message);
        }

        if (!await FileUploadPolicy.HasValidSignatureAsync(request.File!, cancellationToken))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "The file content does not match its extension.");

        IFormFile file = request.File!;
        await using Stream content = file.OpenReadStream();
        StoredFileModel created = await dispatcher.Send(new UploadFileCommand(
            file.FileName,
            file.ContentType,
            content,
            _maximumFileSizeBytes,
            request.Purpose), cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StoredFileModel>>> List(
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<StoredFileModel> files = await dispatcher.Send(new GetFilesQuery(limit), cancellationToken);
        return Ok(files);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<StoredFileModel>> Get(long id, CancellationToken cancellationToken)
    {
        StoredFileModel? file = await dispatcher.Send(new GetFileQuery(id), cancellationToken);
        return file is null ? NotFound() : Ok(file);
    }

    [HttpGet("{id:long}/content")]
    public async Task<IActionResult> Download(long id, CancellationToken cancellationToken)
    {
        StoredFileContent? file = await dispatcher.Send(new OpenFileQuery(id), cancellationToken);
        if (file is null)
            return NotFound();

        return File(file.Content, file.File.ContentType, file.File.OriginalFileName, enableRangeProcessing: true);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        bool deleted = await dispatcher.Send(new DeleteFileCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
