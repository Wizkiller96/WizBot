#nullable disable
namespace WizBot.Services;

public static class RedisImageExtensions
{
    private const string OLD_CDN_URL = "https://wizbot-images.nyc3.digitaloceanspaces.com/";
    private const string NEW_CDN_URL = "cdn.wizbot.cc";

    public static Uri ToNewCdn(this Uri uri)
        => new(uri.ToString().Replace(OLD_CDN_URL, NEW_CDN_URL));
}