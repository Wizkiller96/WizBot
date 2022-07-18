#nullable disable
using Microsoft.EntityFrameworkCore;
using WizBot.Common.ModuleBehaviors;
using WizBot.Db;
using WizBot.Services.Database.Models;

namespace WizBot.Modules.Utility.Services;

public class CommandMapService : IInputTransformer, INService
{
    public ConcurrentDictionary<ulong, ConcurrentDictionary<string, string>> AliasMaps { get; } = new();
    private readonly IEmbedBuilderService _eb;

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

    // todo bank take all
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
            string word;
            var index = input.IndexOf(' ', StringComparison.InvariantCulture);
            if (index == -1)
            {
                word = input;
            }
            else
            {
                word = input[..index];
            }

            string newInput;
            if (maps.TryGetValue(word, out var alias))
            {
                if (index == -1)
                    newInput = alias;
                else
                    newInput = alias + ' ' + input[index..];
            }
            else
            {
                return null;
            }
            
            try
            {
                var toDelete = await channel.SendConfirmAsync(_eb, $"{input} => {newInput}");
                _ = Task.Run(async () =>
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

            // var keys = maps.Keys.OrderByDescending(x => x.Length);
            // foreach (var k in keys)
            // {
            //     string newInput;
            //     if (input.StartsWith(k + " ", StringComparison.InvariantCultureIgnoreCase))
            //         newInput = maps[k] + input.Substring(k.Length, input.Length - k.Length);
            //     else if (input.Equals(k, StringComparison.InvariantCultureIgnoreCase))
            //         newInput = maps[k];
            //     else
            //         continue;
            //
            //     try
            //     {
            //         var toDelete = await channel.SendConfirmAsync(_eb, $"{input} => {newInput}");
            //         _ = Task.Run(async () =>
            //         {
            //             await Task.Delay(1500);
            //             await toDelete.DeleteAsync(new()
            //             {
            //                 RetryMode = RetryMode.AlwaysRetry
            //             });
            //         });
            //     }
            //     catch { }
            //
            //     return newInput;
            // }
        }

        return null;
    }
}