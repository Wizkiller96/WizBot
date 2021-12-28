﻿#nullable disable
using NadekoBot.Common.ModuleBehaviors;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Services.Database.Models;
using NadekoBot.Db;

namespace NadekoBot.Modules.Utility.Services;

public class CommandMapService : IInputTransformer, INService
{
    private readonly IEmbedBuilderService _eb;

    public ConcurrentDictionary<ulong, ConcurrentDictionary<string, string>> AliasMaps { get; } = new();

    private readonly DbService _db;

    //commandmap
    public CommandMapService(DiscordSocketClient client, DbService db, IEmbedBuilderService eb)
    {
        _eb = eb;

        using var uow = db.GetDbContext();
        var guildIds = client.Guilds.Select(x => x.Id).ToList();
        var configs = uow.Set<GuildConfig>()
            .Include(gc => gc.CommandAliases)
            .Where(x => guildIds.Contains(x.GuildId))
            .ToList();
                
        AliasMaps = new(configs
            .ToDictionary(
                x => x.GuildId,
                x => new ConcurrentDictionary<string, string>(x.CommandAliases
                    .DistinctBy(ca => ca.Trigger)
                    .ToDictionary(ca => ca.Trigger, ca => ca.Mapping))));

        _db = db;
    }

    public int ClearAliases(ulong guildId)
    {
        AliasMaps.TryRemove(guildId, out _);

        int count;
        using var uow = _db.GetDbContext();
        var gc = uow.GuildConfigsForId(guildId, set => set.Include(x => x.CommandAliases));
        count = gc.CommandAliases.Count;
        gc.CommandAliases.Clear();
        uow.SaveChanges();
        return count;
    }

    public async Task<string> TransformInput(IGuild guild, IMessageChannel channel, IUser user, string input)
    {
        await Task.Yield();

        if (guild is null || string.IsNullOrWhiteSpace(input))
            return input;

        if (guild != null)
        {
            if (AliasMaps.TryGetValue(guild.Id, out var maps))
            {
                var keys = maps.Keys
                    .OrderByDescending(x => x.Length);

                foreach (var k in keys)
                {
                    string newInput;
                    if (input.StartsWith(k + " ", StringComparison.InvariantCultureIgnoreCase))
                        newInput = maps[k] + input.Substring(k.Length, input.Length - k.Length);
                    else if (input.Equals(k, StringComparison.InvariantCultureIgnoreCase))
                        newInput = maps[k];
                    else
                        continue;

                    try
                    {
                        var toDelete = await channel.SendConfirmAsync(_eb, $"{input} => {newInput}");
                        var _ = Task.Run(async () =>
                        {
                            await Task.Delay(1500);
                            await toDelete.DeleteAsync(new()
                            {
                                RetryMode = RetryMode.AlwaysRetry
                            });
                        });
                    }
                    catch { }
                    return newInput;
                }
            }
        }

        return input;
    }
}
