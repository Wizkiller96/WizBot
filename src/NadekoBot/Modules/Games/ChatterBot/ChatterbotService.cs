#nullable disable
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Db.Models;
using NadekoBot.Modules.Games.Common;
using NadekoBot.Modules.Games.Common.ChatterBot;
using NadekoBot.Modules.Patronage;
using NadekoBot.Modules.Permissions;

namespace NadekoBot.Modules.Games.Services;

public class ChatterBotService : IExecOnMessage
{
    public ConcurrentDictionary<ulong, Lazy<IChatterBotSession>> ChatterBotGuilds { get; }

    public int Priority
        => 1;

    private readonly FeatureLimitKey _flKey;

    private readonly DiscordSocketClient _client;
    private readonly IPermissionChecker _perms;
    private readonly CommandHandler _cmd;
    private readonly IBotCredentials _creds;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IPatronageService _ps;
    private readonly GamesConfigService _gcs;
    private readonly IMessageSenderService _sender;

    public ChatterBotService(
        DiscordSocketClient client,
        IPermissionChecker perms,
        IBot bot,
        CommandHandler cmd,
        IHttpClientFactory factory,
        IBotCredentials creds,
        IPatronageService ps,
        GamesConfigService gcs,
        IMessageSenderService sender)
    {
        _client = client;
        _perms = perms;
        _cmd = cmd;
        _creds = creds;
        _sender = sender;
        _httpFactory = factory;
        _ps = ps;
        _perms = perms;
        _gcs = gcs;

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
        switch (_gcs.Data.ChatBot)
        {
            case ChatBotImplementation.Cleverbot:
                if (!string.IsNullOrWhiteSpace(_creds.CleverbotApiKey))
                    return new OfficialCleverbotSession(_creds.CleverbotApiKey, _httpFactory);

                Log.Information("Cleverbot will not work as the api key is missing.");
                return null;
            case ChatBotImplementation.Gpt3:
                if (!string.IsNullOrWhiteSpace(_creds.Gpt3ApiKey))
                    return new OfficialGpt3Session(_creds.Gpt3ApiKey,
                        _gcs.Data.ChatGpt.ModelName,
                        _gcs.Data.ChatGpt.ChatHistory,
                        _gcs.Data.ChatGpt.MaxTokens,
                        _gcs.Data.ChatGpt.MinTokens,
                        _gcs.Data.ChatGpt.PersonalityPrompt,
                        _client.CurrentUser.Username,
                        _httpFactory);

                Log.Information("Gpt3 will not work as the api key is missing.");
                return null;
            default:
                return null;
        }
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

            var res = await _perms.CheckPermsAsync(sg,
                usrMsg.Channel,
                usrMsg.Author,
                "games",
                CleverBotResponseStr.CLEVERBOT_RESPONSE);

            if (!res.IsAllowed)
                return false;

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
                        await _sender.Response(channel)
                              .Error(null,
                                  text:
                                  "In order to use the cleverbot feature, the owner of this server should be [Patron Tier X](https://patreon.com/join/nadekobot) on patreon.",
                                  footer:
                                  "You may disable the cleverbot feature, and this message via '.cleverbot' command")
                              .SendAsync();

                        return true;
                    }

                    await _sender.Response(channel)
                                 .Error(
                                     null!,
                                     $"You've reached your quota limit of **{ql.Quota}** responses {ql.QuotaPeriod.ToFullName()} for the cleverbot feature.",
                                     footer: "You may wait for the quota reset or .")
                                 .SendAsync();

                    return true;
                }
            }

            _ = channel.TriggerTypingAsync();
            var response = await cbs.Think(message, usrMsg.Author.ToString());
            await _sender.Response(channel)
                         .Confirm(response)
                         .SendAsync();

            Log.Information("""
                            CleverBot Executed
                            Server: {GuildName} [{GuildId}]
                            Channel: {ChannelName} [{ChannelId}]
                            UserId: {Author} [{AuthorId}]
                            Message: {Content}
                            """,
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