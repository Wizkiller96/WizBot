﻿#nullable disable
using Microsoft.EntityFrameworkCore;
using WizBot.Common.ModuleBehaviors;
using WizBot.Db;
using WizBot.Db.Models;

namespace WizBot.Modules.Utility.Services;

public class AliasService : IInputTransformer, INService
{
    public ConcurrentDictionary<ulong, ConcurrentDictionary<string, string>> AliasMaps { get; } = new();

    private readonly DbService _db;
    private readonly IMessageSenderService _sender;

    public AliasService(
        DiscordSocketClient client,
        DbService db,
        IMessageSenderService sender)
    {
        _sender = sender;

        using var uow = db.GetDbContext();
        var guildIds = client.Guilds.Select(x => x.Id).ToList();
        var configs = uow.Set<GuildConfig>()
                         .Include(gc => gc.CommandAliases)
                         .Where(x => guildIds.Contains(x.GuildId))
                         .ToList();

        AliasMaps = new(configs.ToDictionary(x => x.GuildId,
            x => new ConcurrentDictionary<string, string>(x.CommandAliases.DistinctBy(ca => ca.Trigger)
                                                           .ToDictionary(ca => ca.Trigger, ca => ca.Mapping),
                StringComparer.OrdinalIgnoreCase)));

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

    public async Task<string> TransformInput(
        IGuild guild,
        IMessageChannel channel,
        IUser user,
        string input)
    {
        if (guild is null || string.IsNullOrWhiteSpace(input))
            return null;

        if (AliasMaps.TryGetValue(guild.Id, out var maps))
        {
            string newInput = null;
            foreach (var (k, v) in maps)
            {
                if (string.Equals(input, k, StringComparison.OrdinalIgnoreCase))
                {
                    newInput = v;
                }
                else if (input.StartsWith(k + ' ', StringComparison.OrdinalIgnoreCase))
                {
                    if (v.Contains("%target%"))
                        newInput = v.Replace("%target%", input[k.Length..]);
                    else
                        newInput = v + ' ' + input[k.Length..];
                }

                if (newInput is not null)
                {
                    try
                    {
                        var toDelete = await _sender.Response(channel)
                                                    .Confirm($"{input} => {newInput}")
                                                    .SendAsync();
                        toDelete.DeleteAfter(1.5f);
                    }
                    catch
                    {
                        // ignored
                    }

                    return newInput;
                }
            }

            return null;
        }

        return null;
    }
}