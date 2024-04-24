using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Db.Models;

namespace NadekoBot.Modules.Utility;

public sealed class GiveawayService : INService, IReadyExecutor
{
    public static string GiveawayEmoji = "🎉";

    private readonly DbService _db;
    private readonly IBotCredentials _creds;
    private readonly DiscordSocketClient _client;
    private GiveawayModel[] _shardGiveaways;
    private readonly NadekoRandom _rng;

    public GiveawayService(DbService db, IBotCredentials creds, DiscordSocketClient client)
    {
        _db = db;
        _creds = creds;
        _client = client;
        _rng = new NadekoRandom();


        _client.ReactionAdded += OnReactionAdded;
        _client.ReactionRemoved += OnReactionRemoved;
    }

    private async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> msg,
        Cacheable<IMessageChannel, ulong> arg2,
        SocketReaction r)
    {
        if (!r.User.IsSpecified)
            return;
        
        var user = r.User.Value;
        
        if (user.IsBot || user.IsWebhook)
            return;
        
        if (r.Emote is Emoji e && e.Name == GiveawayEmoji)
        {
            await LeaveGivawayAsync(msg.Id, user.Id);
        }
    }

    private async Task OnReactionAdded(Cacheable<IUserMessage, ulong> msg, Cacheable<IMessageChannel, ulong> ch,
        SocketReaction r)
    {
        if (!r.User.IsSpecified)
            return;

        var user = r.User.Value;

        if (user.IsBot || user.IsWebhook)
            return;

        var textChannel = ch.Value as ITextChannel;

        if (textChannel is null)
            return;


        if (r.Emote is Emoji e && e.Name == GiveawayEmoji)
        {
            await JoinGivawayAsync(msg.Id, user.Id, user.Username);
        }
    }

    public async Task OnReadyAsync()
    {
        // load giveaways for this shard from the database
        await using var ctx = _db.GetDbContext();

        _shardGiveaways = await ctx
            .GetTable<GiveawayModel>()
            .Where(x => Linq2DbExpressions.GuildOnShard(x.GuildId, _creds.TotalShards, _client.ShardId))
            .ToArrayAsync();
    }

    public async Task<int?> StartGiveawayAsync(ulong guildId, ulong channelId, ulong messageId, TimeSpan duration,
        string message)
    {
        await using var ctx = _db.GetDbContext();

        // first check if there are more than 5 giveaways
        var count = await ctx
            .GetTable<GiveawayModel>()
            .CountAsync(x => x.GuildId == guildId);

        if (count >= 5)
            return null;

        var endsAt = DateTime.UtcNow + duration;
        var output = await ctx.GetTable<GiveawayModel>()
            .InsertWithOutputAsync(() => new GiveawayModel
            {
                GuildId = guildId,
                MessageId = messageId,
                Message = message,
                EndsAt = endsAt,
            });

        return output.Id;
    }


    public async Task<(GiveawayModel? giveaway, GiveawayUser? winner)> EndGiveawayAsync(ulong guildId, int id)
    {
        await using var ctx = _db.GetDbContext();

        var giveaway = await ctx
            .GetTable<GiveawayModel>()
            .Where(x => x.GuildId == guildId && x.Id == id)
            .LoadWith(x => x.Participants)
            .FirstOrDefaultAsyncLinqToDB();

        if (giveaway is null)
            return default;

        await ctx
            .GetTable<GiveawayModel>()
            .Where(x => x.Id == id)
            .DeleteAsync();

        if (giveaway.Participants.Count == 0)
            return default;
        
        if (giveaway.Participants.Count == 1)
            return (giveaway, giveaway.Participants[0]);

        return (giveaway, giveaway.Participants[_rng.Next(0, giveaway.Participants.Count - 1)]);
    }

    public async Task RerollGiveawayAsync(ulong guildId, int id)
    {
    }

    public async Task<bool> CancelGiveawayAsync(ulong guildId, int id)
    {
        await using var ctx = _db.GetDbContext();

        var count = await ctx
            .GetTable<GiveawayModel>()
            .Where(x => x.GuildId == guildId && x.Id == id)
            .DeleteAsync();

        // todo clear cache

        return count > 0;
    }

    public async Task<IReadOnlyCollection<GiveawayModel>> GetGiveawaysAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();

        return await ctx
            .GetTable<GiveawayModel>()
            .Where(x => x.GuildId == guildId)
            .ToListAsync();
    }

    public async Task<bool> JoinGivawayAsync(ulong messageId, ulong userId, string userName)
    {
        await using var ctx = _db.GetDbContext();

        var giveaway = await ctx
            .GetTable<GiveawayModel>()
            .Where(x => x.MessageId == messageId)
            .FirstOrDefaultAsyncLinqToDB();

        if (giveaway is null)
            return false;

        // add the user to the database
        await ctx.GetTable<GiveawayUser>()
            .InsertAsync(
                () => new GiveawayUser()
                {
                    UserId = userId,
                    GiveawayId = giveaway.Id,
                    Name = userName,
                }
            );

        return true;
    }

    public async Task<bool> LeaveGivawayAsync(ulong messageId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        
        var giveaway = await ctx
            .GetTable<GiveawayModel>()
            .Where(x => x.MessageId == messageId)
            .FirstOrDefaultAsyncLinqToDB();
        
        if (giveaway is null)
            return false;
        
        await ctx
            .GetTable<GiveawayUser>()
            .Where(x => x.UserId == userId && x.GiveawayId == giveaway.Id)
            .DeleteAsync();
        
        return true;
    }
}