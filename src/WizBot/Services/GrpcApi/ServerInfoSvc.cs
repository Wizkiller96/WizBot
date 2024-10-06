using Grpc.Core;

namespace WizBot.GrpcApi;

public sealed class ServerInfoSvc : GrpcInfo.GrpcInfoBase, INService
{
    private readonly IStatsService _stats;

    public ServerInfoSvc(IStatsService stats)
    {
        _stats = stats;
    }

    public override Task<GetServerInfoReply> GetServerInfo(ServerInfoRequest request, ServerCallContext context)
    {
        var info = _stats.GetGuildInfo(request.GuildId);

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

        return Task.FromResult(reply);
    }
}