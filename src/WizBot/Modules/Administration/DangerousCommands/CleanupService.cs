﻿using LinqToDB;
using LinqToDB.Data;
using LinqToDB.EntityFrameworkCore;
using LinqToDB.Mapping;
using WizBot.Common.ModuleBehaviors;
using WizBot.Db.Models;

namespace WizBot.Modules.Administration.DangerousCommands;

public sealed class CleanupService : ICleanupService, IReadyExecutor, INService
{
    private readonly IPubSub _pubSub;
    private TypedKey<KeepReport> _keepReportKey = new("cleanup:report");
    private TypedKey<bool> _keepTriggerKey = new("cleanup:trigger");
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

    public async Task<KeepResult?> DeleteMissingGuildDataAsync()
    {
        guildIds = new();
        var totalShards = _creds.GetCreds().TotalShards;
        await _pubSub.Pub(_keepTriggerKey, true);
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

        foreach (var chunk in allIds.Chunk(20000))
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
        await ctx.GetTable<WizBotExpression>()
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

        // delete ignored users
        await ctx.GetTable<DiscordPermOverride>()
                 .Where(x => x.GuildId != null
                             && !tempTable.Select(x => x.GuildId)
                                          .Contains(x.GuildId.Value))
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

        return new()
        {
            GuildCount = guildIds.Keys.Count,
        };
    }

    public async Task<bool> KeepGuild(ulong guildId)
    {
        await using var db = _db.GetDbContext();
        await using var ctx = db.CreateLinqToDBContext();

        var table = ctx.CreateTable<KeptGuilds>(tableOptions: TableOptions.CheckExistence);
        
        if (await table.AnyAsyncLinqToDB(x => x.GuildId == guildId))
            return false;

        await table.InsertAsync(() => new()
        {
            GuildId = guildId
        });

        return true;
    }

    private ValueTask OnKeepReport(KeepReport report)
    {
        guildIds[report.ShardId] = report.GuildIds;
        return default;
    }

    public async Task OnReadyAsync()
    {
        await _pubSub.Sub(_keepTriggerKey, OnKeepTrigger);

        if (_client.ShardId == 0)
            await _pubSub.Sub(_keepReportKey, OnKeepReport);
    }

    private ValueTask OnKeepTrigger(bool arg)
    {
        _pubSub.Pub(_keepReportKey,
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