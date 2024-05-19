using LinqToDB;
using LinqToDB.Data;
using LinqToDB.EntityFrameworkCore;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Db.Models;

namespace NadekoBot.Modules.Administration.DangerousCommands;

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

        await ctx.GetTable<GuildConfig>()
                 .Where(x => !tempTable.Select(x => x.GuildId)
                                       .Contains(x.GuildId))
                 .DeleteAsync();


        await ctx.GetTable<UserXpStats>()
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