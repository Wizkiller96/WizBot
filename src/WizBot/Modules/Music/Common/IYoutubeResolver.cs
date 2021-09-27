﻿#nullable enable
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WizBot.Modules.Music
{
    public interface IYoutubeResolver : IPlatformQueryResolver
    {
        public Regex YtVideoIdRegex { get; }
        public Task<ITrackInfo?> ResolveByIdAsync(string id);
        IAsyncEnumerable<ITrackInfo> ResolveTracksFromPlaylistAsync(string query);
        Task<ITrackInfo?> ResolveByQueryAsync(string query, bool tryExtractingId);
    }
}