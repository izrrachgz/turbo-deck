namespace Api.Modules.Playback.Requests;

public sealed record ClearPlaybackQueueRequest(bool StopCurrent = false);
