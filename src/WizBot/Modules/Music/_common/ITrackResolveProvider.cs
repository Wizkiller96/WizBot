﻿namespace WizBot.Modules.Music;

public interface ITrackResolveProvider
{
    Task<ITrackInfo?> QuerySongAsync(string query, MusicPlatform? forcePlatform);
}