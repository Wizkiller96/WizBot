﻿using System;

namespace WizBot.Services
{
    public static class RedisImageExtensions
    {
        private const string OldCdnUrl = "nadeko-pictures.nyc3.digitaloceanspaces.com";
        private const string NewCdnUrl = "cdn.wizbot.cc";
        
        public static Uri ToNewCdn(this Uri uri)
            => new(uri.ToString().Replace(OldCdnUrl, NewCdnUrl));
    }
}