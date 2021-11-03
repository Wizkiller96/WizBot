using System;

namespace NadekoBot.Services
{
    public static class RedisImageExtensions
    {
        private const string OldCdnUrl = "nadeko-pictures.nyc3.digitaloceanspaces.com";
        private const string NewCdnUrl = "cdn.nadeko.bot";
        
        public static Uri ToNewCdn(this Uri uri)
            => new(uri.ToString().Replace(OldCdnUrl, NewCdnUrl));
    }
}