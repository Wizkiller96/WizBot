using Grpc.Core;
using GreetType = WizBot.Services.GreetType;

namespace WizBot.GrpcApi;

public sealed class GreetByeSvc : GrpcGreet.GrpcGreetBase, INService
{
    private readonly GreetService _gs;
    private readonly DiscordSocketClient _client;

    public GreetByeSvc(GreetService gs, DiscordSocketClient client)
    {
        _gs = gs;
        _client = client;
    }

    private static GrpcGreetSettings ToConf(GreetSettings? conf)
    {
        if (conf is null)
            return new GrpcGreetSettings();

        return new GrpcGreetSettings()
        {
            Message = conf.MessageText,
            Type = (GrpcGreetType)conf.GreetType,
            ChannelId = conf.ChannelId?.ToString() ?? string.Empty,
            IsEnabled = conf.IsEnabled,
        };
    }
    
    public override async Task<GrpcGreetSettings> GetGreetSettings(GetGreetRequest request, ServerCallContext context)
    {
        var guildId = request.GuildId;

        var conf = await _gs.GetGreetSettingsAsync(guildId, (GreetType)request.Type);

        return ToConf(conf);
    }
    
    public override async Task<UpdateGreetReply> UpdateGreet(UpdateGreetRequest request, ServerCallContext context)
    {
        var gid = request.GuildId;
        var s = request.Settings;
        var msg = s.Message;

        var type = GetGreetType(s.Type);

        await _gs.SetMessage(gid, GetGreetType(s.Type), msg);
        await _gs.SetGreet(gid, ulong.Parse(s.ChannelId), type, s.IsEnabled);
        var settings = await _gs.GetGreetSettingsAsync(gid, type);

        if (settings is null)
            return new();
        
        return new()
        {
            Settings = ToConf(settings)
        };
    }
    
    public override Task<TestGreetReply> TestGreet(TestGreetRequest request, ServerCallContext context)
        => TestGreet(request.GuildId, request.ChannelId, request.UserId, request.Type);

    private async Task<TestGreetReply> TestGreet(
        ulong guildId,
        ulong channelId,
        ulong userId,
        GrpcGreetType gtDto)
    {
        var g = _client.GetGuild(guildId) as IGuild;
        if (g is null)
        {
            return new()
            {
                Error = "Guild doesn't exist",
                Success = false,
            };
        }

        var gu = await g.GetUserAsync(userId);
        var ch = await g.GetTextChannelAsync(channelId);

        if (gu is null || ch is null)
            return new TestGreetReply()
            {
                Error = "Guild or channel doesn't exist",
                Success = false,
            };


        var gt = GetGreetType(gtDto);

        await _gs.Test(guildId, gt, ch, gu);
        return new TestGreetReply()
        {
            Success = true
        };
    }

    private static GreetType GetGreetType(GrpcGreetType gtDto)
    {
        return gtDto switch
        {
            GrpcGreetType.Greet => GreetType.Greet,
            GrpcGreetType.GreetDm => GreetType.GreetDm,
            GrpcGreetType.Bye => GreetType.Bye,
            GrpcGreetType.Boost => GreetType.Boost,
            _ => throw new ArgumentOutOfRangeException(nameof(gtDto), gtDto, null)
        };
    }
}