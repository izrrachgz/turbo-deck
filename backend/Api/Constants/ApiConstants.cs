namespace Api.Constants;

internal static class ApiConstants
{
    public const string Section = "Api";
    public const string ControlTokenHeader = "X-Turbo-Deck-Control-Token";
    public const string ApiKeyHeader = "X-Turbo-Deck-Api-Key";
    public const string RealtimeHubPath = "/hubs/realtime";
    public const string VisualizerHubPath = "/hubs/visualizer";
    public const string DeckExplorerHubPath = "/hubs/deck-explorer";
    public const string ReceiveOnlyPlaybackAccessItem = "TurboDeck.ReceiveOnlyPlaybackAccess";
    public const string VisualizerPath = "/visualizer";
    public const string VisualizerApiPath = "/api/v1/visualizer";
    public const string DeckExplorerPath = "/deck";
    public const string DeckExplorerApiPath = "/api/v1/deck-explorer";
    public const string TwitchApiPath = "/api/v1/twitch";
    public const int DefaultPort = 52024;
    public const int RestartRequestedExitCode = 23;
}
