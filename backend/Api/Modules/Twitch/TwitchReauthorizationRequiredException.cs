namespace Api.Modules.Twitch;

internal sealed class TwitchReauthorizationRequiredException(Exception innerException) : Exception(UserMessage, innerException)
{
    public const string UserMessage =
        "Twitch could not refresh this authorization. Set the Twitch application's OAuth Client Type to Public, " +
        "then disconnect and connect again. Turbo Deck does not store client secrets.";
}
