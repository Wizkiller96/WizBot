﻿using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using WizBot.Modules.Gambling.Services;
using WizBot.Modules.Xp.Services;

namespace WizBot.GrpcApi;

public static class GrpcApiExtensions
{
    public static ulong GetUserId(this ServerCallContext context)
        => ulong.Parse(context.RequestHeaders.FirstOrDefault(x => x.Key == "userid")!.Value);
}

public sealed class OtherSvc : GrpcOther.GrpcOtherBase, IGrpcSvc, INService
{
    private readonly IDiscordClient _client;
    private readonly XpService _xp;
    private readonly ICurrencyService _cur;
    private readonly WaifuService _waifus;
    private readonly ICoordinator _coord;
    private readonly IStatsService _stats;
    private readonly IBotCache _cache;

    public OtherSvc(
        DiscordSocketClient client,
        XpService xp,
        ICurrencyService cur,
        WaifuService waifus,
        ICoordinator coord,
        IStatsService stats,
        IBotCache cache)
    {
        _client = client;
        _xp = xp;
        _cur = cur;
        _waifus = waifus;
        _coord = coord;
        _stats = stats;
        _cache = cache;
    }
    
    public ServerServiceDefinition Bind()
        => GrpcOther.BindService(this);

    [GrpcNoAuthRequired]
    public override async Task<BotOnGuildReply> BotOnGuild(BotOnGuildRequest request, ServerCallContext context)
    {
        var guild = await _client.GetGuildAsync(request.GuildId);
        
        var reply = new BotOnGuildReply
        {
            Success = guild is not null
        };
        
        return reply;
    }

    public override async Task<GetRolesReply> GetRoles(GetRolesRequest request, ServerCallContext context)
    {
        var g = await _client.GetGuildAsync(request.GuildId);
        var roles = g?.Roles;
        var reply = new GetRolesReply();
        reply.Roles.AddRange(roles?.Select(x => new RoleReply()
        {
            Id = x.Id,
            Name = x.Name,
            Color = x.Color.ToString(),
            IconUrl = x.GetIconUrl() ?? string.Empty,
        }) ?? new List<RoleReply>());

        return reply;
    }
    
    public override async Task<GetTextChannelsReply> GetTextChannels(
        GetTextChannelsRequest request,
        ServerCallContext context)
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

    [GrpcNoAuthRequired]
    public override async Task<GetGuildsReply> GetGuilds(Empty request, ServerCallContext context)
    {
        var guilds = await _client.GetGuildsAsync(CacheMode.CacheOnly);

        var reply = new GetGuildsReply();
        var userId = context.GetUserId();

        var toReturn = new List<IGuild>();
        foreach (var g in guilds)
        {
            var user = await g.GetUserAsync(userId);
            if (user is not null && user.GuildPermissions.Has(GuildPermission.Administrator))
                toReturn.Add(g);
        }

        reply.Guilds.AddRange(toReturn
            .Select(x => new GuildReply()
            {
                Id = x.Id,
                Name = x.Name,
                IconUrl = x.IconUrl
            }));

        return reply;
    }

    
    [GrpcNoAuthRequired]
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
                User = user?.ToString() ?? x.Username,
                UserId = x.UserId,
                Avatar = user?.RealAvatarUrl().ToString() ?? x.RealAvatarUrl()?.ToString()
            };
        });

        reply.Entries.AddRange(await entries.WhenAll());

        return reply;
    }

    [GrpcNoAuthRequired]
    public override async Task<XpLbReply> GetXpLb(GetLbRequest request, ServerCallContext context)
    {
        var users = await _xp.GetGlobalUserXps(request.Page);

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

    [GrpcNoAuthRequired]
    public override async Task<WaifuLbReply> GetWaifuLb(GetLbRequest request, ServerCallContext context)
    {
        var waifus = await _waifus.GetTopWaifusAtPage(request.Page, request.PerPage);

        var reply = new WaifuLbReply();
        reply.Entries.AddRange(waifus.Select(x => new WaifuLbEntry()
        {
            ClaimedBy = x.ClaimerName ?? string.Empty,
            IsMutual = x.ClaimerName == x.Affinity,
            Value = x.Price,
            User = x.WaifuName,
        }));
        return reply;
    }

    [GrpcNoAuthRequired]
    public override async Task<GetShardStatusesReply> GetShardStatuses(Empty request, ServerCallContext context)
    {
        var reply = new GetShardStatusesReply();

        await _cache.GetOrAddAsync<List<ShardStatus>>("coord:statuses",
            () => Task.FromResult(_coord.GetAllShardStatuses().ToList())!,
            TimeSpan.FromMinutes(1));

        var shards = _coord.GetAllShardStatuses();

        reply.Shards.AddRange(shards.Select(x => new ShardStatusReply()
        {
            Id = x.ShardId,
            Status = x.ConnectionState.ToString(),
            GuildCount = x.GuildCount,
            LastUpdate = Timestamp.FromDateTime(x.LastUpdate),
        }));
        

        return reply;
    }
    
    public override async Task<GetServerInfoReply> GetServerInfo(ServerInfoRequest request, ServerCallContext context)
    {
        var info = await _stats.GetGuildInfoAsync(request.GuildId);

        var reply = new GetServerInfoReply()
        {
            Id = info.Id,
            Name = info.Name,
            IconUrl = info.IconUrl,
            OwnerId = info.OwnerId,
            OwnerName = info.Owner,
            TextChannels = info.TextChannels,
            VoiceChannels = info.VoiceChannels,
            MemberCount = info.MemberCount,
            CreatedAt = info.CreatedAt.Ticks,
        };

        reply.Features.AddRange(info.Features);
        reply.Emojis.AddRange(info.Emojis.Select(x => new EmojiReply()
        {
            Name = x.Name,
            Url = x.Url,
            Code = x.ToString()
        }));

        reply.Roles.AddRange(info.Roles.Select(x => new RoleReply()
        {
            Id = x.Id,
            Name = x.Name,
            IconUrl = x.GetIconUrl() ?? string.Empty,
            Color = x.Color.ToString()
        }));

        return reply;
    }
}