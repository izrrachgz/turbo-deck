namespace Api.Modules.Deck.Requests;

public sealed class ImportProfileRequest
{
    public IFormFile? File { get; init; }
}
