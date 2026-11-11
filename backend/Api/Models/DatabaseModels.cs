namespace Api.Models;

public sealed record DatabaseEngineStateResponse(
    string Engine,
    string DisplayName,
    string Status,
    bool Configured,
    string? ConnectionSummary,
    string? LastError);

public sealed record DatabaseSettingsResponse(string ActiveEngine, IReadOnlyList<DatabaseEngineStateResponse> Engines);

public sealed record DatabaseConnectionRequest(string Engine, string? ConnectionString);

public sealed record DatabaseConnectionTestResponse(bool Success, string Status, string Message, string? ConnectionSummary);

public sealed record DatabaseSwitchResponse(string ActiveEngine, string Status, string Message);
