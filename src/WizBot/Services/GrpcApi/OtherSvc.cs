using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using WizBot.Modules.Gambling.Services;
using WizBot.Modules.Xp.Services;

namespace WizBot.GrpcApi;

public sealed class OtherSvc : GrpcOther.GrpcOtherBase, INService
{
    private readonly IDiscordClient _client;
    private readonly XpService _xp;
    private readonly ICurrencyService _cur;
    private readonly WaifuService _waifus;
    private readonly ICoordinator _coord;

    public OtherSvc(
        DiscordSocketClient client,
        XpService xp,
        ICurrencyService cur,
        WaifuService waifus,
        ICoordinator coord)
    {
        _client = client;
        _xp = xp;
        _cur = cur;
        _waifus = waifus;
        _coord = coord;
    }

    public override async Task<GetTextChannelsReply> GetTextChannels(GetTextChannelsRequest request, ServerCallContext context)
    {
        var g = await _client.GetGuildAsync(request.GuildId);
        var reply = new GetTextChannelsReply();

        var chs = await g.GetTextChannelsAsync();

        reply.TextChannels.AddRange(chs.Select(x => new TextChannelReply()
        {
            Id = x.Id,
            Name = x.Name,
        }));

        return reply;
    }

    public override async Task<CurrencyLbReply> GetCurrencyLb(GetLbRequest request, ServerCallContext context)
    {
        var users = await _cur.GetTopRichest(_client.CurrentUser.Id, request.Page, request.PerPage);

        var reply = new CurrencyLbReply();
        var entries = users.Select(async x =>
        {
            var user = await _client.GetUserAsync(x.UserId, CacheMode.CacheOnly);
            return new CurrencyLbEntryReply()
            {
                Amount = x.CurrencyAmount,
                User = user.ToString(),
                UserId = x.UserId,
                Avatar = user.RealAvatarUrl().ToString()
            };
        });

        reply.Entries.AddRange(await entries.WhenAll());

        return reply;
    }

    public override async Task<XpLbReply> GetXpLb(GetLbRequest request, ServerCallContext context)
    {
        var users = await _xp.GetUserXps(request.Page, request.PerPage);

        var reply = new XpLbReply();

        var entries = users.Select(x =>
        {
            var lvl = new LevelStats(x.TotalXp);

            return new XpLbEntryReply()
            {
                Level = lvl.Level,
                TotalXp = x.TotalXp,
                User = x.Username,
                UserId = x.UserId
            };
        });

        reply.Entries.AddRange(entries);

        return reply;
    }

    public override async Task<WaifuLbReply> GetWaifuLb(GetLbRequest request, ServerCallContext context)
    {
        var waifus = await _waifus.GetTopWaifusAtPage(request.Page, request.PerPage);

        var reply = new WaifuLbReply();
        reply.Entries.AddRange(waifus.Select(x => new WaifuLbEntry()
        {
            ClaimedBy = x.Claimer,
            IsMutual = x.Claimer == x.Affinity,
            Value = x.Price,
            User = x.Username,
        }));
        return reply;
    }

    public override Task<GetShardStatusesReply> GetShardStatuses(Empty request, ServerCallContext context)
    {
        var reply = new GetShardStatusesReply();

        var shards = _coord.GetAllShardStatuses();

        reply.Shards.AddRange(shards.Select(x => new ShardStatusReply()
        {
            Id = x.ShardId,
            Status = x.ConnectionState.ToString(),
            GuildCount = x.GuildCount,
            LastUpdate = Timestamp.FromDateTime(x.LastUpdate),
        }));

        return Task.FromResult(reply);
    }
}