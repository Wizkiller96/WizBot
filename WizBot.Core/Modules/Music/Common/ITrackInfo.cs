﻿#nullable enable
using System;
using System.Threading.Tasks;

namespace WizBot.Core.Modules.Music
{
    public interface ITrackInfo
    {
        public string Title { get; }
        public string Url { get; }
        public string Thumbnail { get; }
        public TimeSpan Duration { get; }
        public MusicPlatform Platform { get; }
        public ValueTask<string?> GetStreamUrl();
    }
}