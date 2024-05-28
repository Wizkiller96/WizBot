﻿#nullable disable
using Microsoft.EntityFrameworkCore;
using WizBot.Db.Models;

namespace WizBot.Db;

public static class MusicPlayerSettingsExtensions
{
    public static async Task<MusicPlayerSettings> ForGuildAsync(this DbSet<MusicPlayerSettings> settings, ulong guildId)
    {
        var toReturn = await settings.AsQueryable().FirstOrDefaultAsync(x => x.GuildId == guildId);

        if (toReturn is null)
        {
            var newSettings = new MusicPlayerSettings
            {
                GuildId = guildId,
                PlayerRepeat = PlayerRepeatType.Queue
            };

            await settings.AddAsync(newSettings);
            return newSettings;
        }

        return toReturn;
    }
}