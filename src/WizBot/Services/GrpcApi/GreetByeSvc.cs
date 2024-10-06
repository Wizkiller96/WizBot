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

    public GreetSettings GetDefaultGreet(GreetType type)
        => new GreetSettings()
        {
            GreetType = type
        };

    private static GrpcGreetSettings ToConf(GreetSettings? conf)
    {
        if (conf is null)
            return new GrpcGreetSettings();

        return new GrpcGreetSettings()
        {
            Message = conf.MessageText,
            Type = (GrpcGreetType)conf.GreetType,
            ChannelId = conf.ChannelId ?? 0,
            IsEnabled = conf.IsEnabled,
        };
    }

    [GrpcApiPerm(GuildPerm.Administrator)]
    public override async Task<GetGreetReply> GetGreetSettings(GetGreetRequest request, ServerCallContext context)
    {
        var guildId = request.GuildId;

        var greetConf = await _gs.GetGreetSettingsAsync(guildId, GreetType.Greet);
        var byeConf = await _gs.GetGreetSettingsAsync(guildId, GreetType.Bye);
        var boostConf = await _gs.GetGreetSettingsAsync(guildId, GreetType.Boost);
        var greetDmConf = await _gs.GetGreetSettingsAsync(guildId, GreetType.GreetDm);
        // todo timer

        return new GetGreetReply()
        {
            Greet = ToConf(greetConf),
            Bye = ToConf(byeConf),
            Boost = ToConf(boostConf),
            GreetDm = ToConf(greetDmConf)
        };
    }

    [GrpcApiPerm(GuildPerm.Administrator)]
    public override async Task<UpdateGreetReply> UpdateGreet(UpdateGreetRequest request, ServerCallContext context)
    {
        var gid = request.GuildId;
        var s = request.Settings;
        var msg = s.Message;

        await _gs.SetMessage(gid, GetGreetType(s.Type), msg);
        await _gs.SetGreet(gid, s.ChannelId, GetGreetType(s.Type), s.IsEnabled);

        return new()
        {
            Success = true
        };
    }

    [GrpcApiPerm(GuildPerm.Administrator)]
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