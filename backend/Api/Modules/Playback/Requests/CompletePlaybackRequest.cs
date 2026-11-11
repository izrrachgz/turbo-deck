namespace Api.Modules.Playback.Requests;

public sealed record CompletePlaybackRequest(Guid ItemId, bool Success = true, string? ErrorCode = null);
