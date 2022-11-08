#nullable disable
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Db.Models;
using NadekoBot.Modules.Games.Common.ChatterBot;
using NadekoBot.Modules.Permissions;
using NadekoBot.Modules.Permissions.Common;
using NadekoBot.Modules.Permissions.Services;
using NadekoBot.Modules.Utility.Patronage;

namespace NadekoBot.Modules.Games.Services;

public class ChatterBotService : IExecOnMessage
{
    public ConcurrentDictionary<ulong, Lazy<IChatterBotSession>> ChatterBotGuilds { get; }

    public int Priority
        => 1;

    private readonly FeatureLimitKey _flKey;

    private readonly DiscordSocketClient _client;
    private readonly PermissionService _perms;
    private readonly CommandHandler _cmd;
    private readonly IBotStrings _strings;
    private readonly IBotCredentials _creds;
    private readonly IEmbedBuilderService _eb;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IPatronageService _ps;
    private readonly CmdCdService _ccs;

    public ChatterBotService(
        DiscordSocketClient client,
        PermissionService perms,
        Bot bot,
        CommandHandler cmd,
        IBotStrings strings,
        IHttpClientFactory factory,
        IBotCredentials creds,
        IEmbedBuilderService eb,
        IPatronageService ps,
        CmdCdService cmdCdService)
    {
        _client = client;
        _perms = perms;
        _cmd = cmd;
        _strings = strings;
        _creds = creds;
        _eb = eb;
        _httpFactory = factory;
        _ps = ps;
        _ccs = cmdCdService;

        _flKey = new FeatureLimitKey()
        {
            Key = CleverBotResponseStr.CLEVERBOT_RESPONSE,
            PrettyName = "Cleverbot Replies"
        };

        ChatterBotGuilds = new(bot.AllGuildConfigs
                                  .Where(gc => gc.CleverbotEnabled)
                                  .ToDictionary(gc => gc.GuildId,
                                      _ => new Lazy<IChatterBotSession>(() => CreateSession(), true)));
    }

    public IChatterBotSession CreateSession()
    {
        if (!string.IsNullOrWhiteSpace(_creds.CleverbotApiKey))
            return new OfficialCleverbotSession(_creds.CleverbotApiKey, _httpFactory);

        Log.Information("Cleverbot will not work as the api key is missing.");
        return null;
    }

    public string PrepareMessage(IUserMessage msg, out IChatterBotSession cleverbot)
    {
        var channel = msg.Channel as ITextChannel;
        cleverbot = null;

        if (channel is null)
            return null;

        if (!ChatterBotGuilds.TryGetValue(channel.Guild.Id, out var lazyCleverbot))
            return null;

        cleverbot = lazyCleverbot.Value;

        var nadekoId = _client.CurrentUser.Id;
        var normalMention = $"<@{nadekoId}> ";
        var nickMention = $"<@!{nadekoId}> ";
        string message;
        if (msg.Content.StartsWith(normalMention, StringComparison.InvariantCulture))
            message = msg.Content[normalMention.Length..].Trim();
        else if (msg.Content.StartsWith(nickMention, StringComparison.InvariantCulture))
            message = msg.Content[nickMention.Length..].Trim();
        else
            return null;

        return message;
    }

    public async Task<bool> ExecOnMessageAsync(IGuild guild, IUserMessage usrMsg)
    {
        if (guild is not SocketGuild sg)
            return false;
        
        try
        {
            var message = PrepareMessage(usrMsg, out var cbs);
            if (message is null || cbs is null)
                return false;

            var pc = _perms.GetCacheFor(guild.Id);
            if (!pc.Permissions.CheckPermissions(usrMsg,
                    "cleverbot",
                    "games",
                    out var index))
            {
                if (pc.Verbose)
                {
                    var returnMsg = _strings.GetText(strs.perm_prevent(index + 1,
                        Format.Bold(pc.Permissions[index].GetCommand(_cmd.GetPrefix(sg), sg))));

                    try { await usrMsg.Channel.SendErrorAsync(_eb, returnMsg); }
                    catch { }

                    Log.Information("{PermissionMessage}", returnMsg);
                }

                return false;
            }

            if (await _ccs.TryBlock(sg, usrMsg.Author, CleverBotResponseStr.CLEVERBOT_RESPONSE))
            {
                return true;
            }

            var channel = (ITextChannel)usrMsg.Channel;
            var conf = _ps.GetConfig();
            if (!_creds.IsOwner(sg.OwnerId) && conf.IsEnabled)
            {
                var quota = await _ps.TryGetFeatureLimitAsync(_flKey, sg.OwnerId, 0);

                uint? daily = quota.Quota is int dVal and < 0
                    ? (uint)-dVal
                    : null;

                uint? monthly = quota.Quota is int mVal and >= 0
                    ? (uint)mVal
                    : null;
                
                var maybeLimit = await _ps.TryIncrementQuotaCounterAsync(sg.OwnerId,
                    sg.OwnerId == usrMsg.Author.Id,
                    FeatureType.Limit,
                    _flKey.Key,
                    null,
                    daily,
                    monthly);
                
                if (maybeLimit.TryPickT1(out var ql, out var counters))
                {
                    if (ql.Quota == 0)
                    {
                        await channel.SendErrorAsync(_eb,
                            null!,
                            text:
                            "In order to use the cleverbot feature, the owner of this server should be [Patron Tier X](https://patreon.com/join/nadekobot) on patreon.",
                            footer:
                            "You may disable the cleverbot feature, and this message via '.cleverbot' command");
                        
                        return true;
                    }

                    await channel.SendErrorAsync(_eb,
                        null!,
                        $"You've reached your quota limit of **{ql.Quota}** responses {ql.QuotaPeriod.ToFullName()} for the cleverbot feature.",
                        footer: "You may wait for the quota reset or .");
                    
                    return true;
                }
            }

            _ = channel.TriggerTypingAsync();
            var response = await cbs.Think(message);
            await channel.SendConfirmAsync(_eb,
                title: null,
                response.SanitizeMentions(true)
                // , footer: counter > 0 ? counter.ToString() : null
                );

            Log.Information(@"CleverBot Executed
Server: {GuildName} [{GuildId}]
Channel: {ChannelName} [{ChannelId}]
UserId: {Author} [{AuthorId}]
Message: {Content}",
                guild.Name,
                guild.Id,
                usrMsg.Channel?.Name,
                usrMsg.Channel?.Id,
                usrMsg.Author,
                usrMsg.Author.Id,
                usrMsg.Content);

            return true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error in cleverbot");
        }

        return false;
    }
}