using LinqToDB;
using LinqToDB.Data;
using LinqToDB.EntityFrameworkCore;
using LinqToDB.Mapping;
using WizBot.Common.ModuleBehaviors;
using WizBot.Db.Models;

namespace WizBot.Modules.Administration.DangerousCommands;

public sealed class CleanupService : ICleanupService, IReadyExecutor, INService
{
    private TypedKey<KeepReport> _cleanupReportKey = new("cleanup:report");
    private TypedKey<bool> _cleanupTriggerKey = new("cleanup:trigger");

    private readonly IPubSub _pubSub;
    private readonly DiscordSocketClient _client;
    private ConcurrentDictionary<int, ulong[]> guildIds = new();
    private readonly IBotCredsProvider _creds;
    private readonly DbService _db;

    public CleanupService(
        IPubSub pubSub,
        DiscordSocketClient client,
        IBotCredsProvider creds,
        DbService db)
    {
        _pubSub = pubSub;
        _client = client;
        _creds = creds;
        _db = db;
    }

    public async Task OnReadyAsync()
    {
        await _pubSub.Sub(_cleanupTriggerKey, OnCleanupTrigger);

        if (_client.ShardId == 0)
            await _pubSub.Sub(_cleanupReportKey, OnKeepReport);
    }

    public async Task<KeepResult?> DeleteMissingGuildDataAsync()
    {
        guildIds = new();
        var totalShards = _creds.GetCreds().TotalShards;
        await _pubSub.Pub(_cleanupTriggerKey, true);
        var counter = 0;
        while (guildIds.Keys.Count < totalShards)
        {
            await Task.Delay(1000);
            counter++;

            if (counter >= 5)
                break;
        }

        if (guildIds.Keys.Count < totalShards)
            return default;

        var allIds = guildIds.SelectMany(x => x.Value)
                             .ToArray();

        await using var ctx = _db.GetDbContext();
        await using var linqCtx = ctx.CreateLinqToDBContext();
        await using var tempTable = linqCtx.CreateTempTable<CleanupId>();

        foreach (var chunk in allIds.Chunk(10000))
        {
            await tempTable.BulkCopyAsync(chunk.Select(x => new CleanupId()
            {
                GuildId = x
            }));
        }

        // delete guild configs
        await ctx.GetTable<GuildConfig>()
                 .Where(x => !tempTable.Select(x => x.GuildId)
                                       .Contains(x.GuildId))
                 .DeleteAsync();

        // delete guild xp
        await ctx.GetTable<UserXpStats>()
                 .Where(x => !tempTable.Select(x => x.GuildId)
                                       .Contains(x.GuildId))
                 .DeleteAsync();

        // delete expressions
        await ctx.GetTable<WizExpression>()
                 .Where(x => x.GuildId != null
                             && !tempTable.Select(x => x.GuildId)
                                          .Contains(x.GuildId.Value))
                 .DeleteAsync();

        // delete quotes
        await ctx.GetTable<Quote>()
                 .Where(x => !tempTable.Select(x => x.GuildId)
                                       .Contains(x.GuildId))
                 .DeleteAsync();

        // delete planted currencies
        await ctx.GetTable<PlantedCurrency>()
                 .Where(x => !tempTable.Select(x => x.GuildId)
                                       .Contains(x.GuildId))
                 .DeleteAsync();

        // delete image only channels
        await ctx.GetTable<ImageOnlyChannel>()
                 .Where(x => !tempTable.Select(x => x.GuildId)
                                       .Contains(x.GuildId))
                 .DeleteAsync();

        // delete reaction roles
        await ctx.GetTable<ReactionRoleV2>()
                 .Where(x => !tempTable.Select(x => x.GuildId)
                                       .Contains(x.GuildId))
                 .DeleteAsync();

        // delete perm overrides
        await ctx.GetTable<DiscordPermOverride>()
                 .Where(x => x.GuildId != null
                             && !tempTable.Select(x => x.GuildId)
                                          .Contains(x.GuildId.Value))
                 .DeleteAsync();

        // delete repeaters
        await ctx.GetTable<Repeater>()
                 .Where(x => !tempTable.Select(x => x.GuildId)
                                       .Contains(x.GuildId))
                 .DeleteAsync();
        
        // delete autopublish channels
        await ctx.GetTable<AutoPublishChannel>()
                 .Where(x => !tempTable.Select(x => x.GuildId)
                                       .Contains(x.GuildId))
                 .DeleteAsync();
        
        // delete greet settings
        await ctx.GetTable<GreetSettings>()
                 .Where(x => !tempTable.Select(x => x.GuildId)
                                       .Contains(x.GuildId))
                 .DeleteAsync();
        
        // delete sar
        await ctx.GetTable<SarGroup>()
                 .Where(x => !tempTable.Select(x => x.GuildId)
                                       .Contains(x.GuildId))
                 .DeleteAsync();
        
        // delete warnings
        await ctx.GetTable<Warning>()
                 .Where(x => !tempTable.Select(x => x.GuildId)
                                       .Contains(x.GuildId))
                 .DeleteAsync();
        
        // delete warn punishments
        await ctx.GetTable<WarningPunishment>()
                 .Where(x => !tempTable.Select(x => x.GuildId)
                                       .Contains(x.GuildId))
                 .DeleteAsync(); 
        
        // delete sticky roles
        await ctx.GetTable<StickyRole>()
                 .Where(x => !tempTable.Select(x => x.GuildId)
                                       .Contains(x.GuildId))
                 .DeleteAsync();
        
        // delete at channels
        await ctx.GetTable<AutoTranslateChannel>()
                 .Where(x => !tempTable.Select(x => x.GuildId)
                                       .Contains(x.GuildId))
                 .DeleteAsync();
        
        // delete ban templates
        await ctx.GetTable<BanTemplate>()
                 .Where(x => !tempTable.Select(x => x.GuildId)
                                       .Contains(x.GuildId))
                 .DeleteAsync();
        
        // delete reminders
        await ctx.GetTable<Reminder>()
                 .Where(x => !tempTable.Select(x => x.GuildId)
                                       .Contains(x.ServerId))
                 .DeleteAsync();
        
        // delete button roles
        await ctx.GetTable<ButtonRole>()
                 .Where(x => !tempTable.Select(x => x.GuildId)
                                       .Contains(x.GuildId))
                 .DeleteAsync();
        
        return new()
        {
            GuildCount = guildIds.Keys.Count,
        };
    }

    private ValueTask OnKeepReport(KeepReport report)
    {
        guildIds[report.ShardId] = report.GuildIds;
        return default;
    }

    private ValueTask OnCleanupTrigger(bool arg)
    {
        _pubSub.Pub(_cleanupReportKey,
            new KeepReport()
            {
                ShardId = _client.ShardId,
                GuildIds = _client.GetGuildIds(),
            });

        return default;
    }
}

public class KeptGuilds
{
    [PrimaryKey]
    public ulong GuildId { get; set; }
}