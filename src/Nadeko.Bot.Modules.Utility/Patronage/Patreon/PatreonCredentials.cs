#nullable disable
namespace NadekoBot.Modules.Utility;

public readonly struct PatreonCredentials
{
    public string ClientId { get; init; }
    public string ClientSecret { get; init; }
    public string AccessToken { get; init; }
    public string RefreshToken { get; init; }
}