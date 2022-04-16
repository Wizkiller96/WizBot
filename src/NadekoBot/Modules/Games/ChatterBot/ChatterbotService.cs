#nullable disable
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Modules.Games.Common.ChatterBot;
using NadekoBot.Modules.Permissions.Common;
using NadekoBot.Modules.Permissions.Services;

namespace NadekoBot.Modules.Games.Services;

public class ChatterBotService : IExecOnMessage
{
    public ConcurrentDictionary<ulong, Lazy<IChatterBotSession>> ChatterBotGuilds { get; }

    public int Priority
        => 1;

    private readonly DiscordSocketClient _client;
    private readonly PermissionService _perms;
    private readonly CommandHandler _cmd;
    private readonly IBotStrings _strings;
    private readonly IBotCredentials _creds;
    private readonly IEmbedBuilderService _eb;
    private readonly IHttpClientFactory _httpFactory;

    public ChatterBotService(
        DiscordSocketClient client,
        PermissionService perms,
        Bot bot,
        CommandHandler cmd,
        IBotStrings strings,
        IHttpClientFactory factory,
        IBotCredentials creds,
        IEmbedBuilderService eb)
    {
        _client = client;
        _perms = perms;
        _cmd = cmd;
        _strings = strings;
        _creds = creds;
        _eb = eb;
        _httpFactory = factory;

        ChatterBotGuilds = new(bot.AllGuildConfigs.Where(gc => gc.CleverbotEnabled)
                                  .ToDictionary(gc => gc.GuildId,
                                      _ => new Lazy<IChatterBotSession>(() => CreateSession(), true)));
    }

    public IChatterBotSession CreateSession()
    {
        if (!string.IsNullOrWhiteSpace(_creds.CleverbotApiKey))
            return new OfficialCleverbotSession(_creds.CleverbotApiKey, _httpFactory);
        return new CleverbotIoSession("GAh3wUfzDCpDpdpT", "RStKgqn7tcO9blbrv4KbXM8NDlb7H37C", _httpFactory);
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

    public async Task<bool> TryAsk(IChatterBotSession cleverbot, ITextChannel channel, string message)
    {
        await channel.TriggerTypingAsync();

        var response = await cleverbot.Think(message);
        try
        {
            await channel.SendConfirmAsync(_eb, response.SanitizeMentions(true));
        }
        catch
        {
            await channel.SendConfirmAsync(_eb, response.SanitizeMentions(true)); // try twice :\
        }

        return true;
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
            if (!pc.Permissions.CheckPermissions(usrMsg, "cleverbot", "Games".ToLowerInvariant(), out var index))
            {
                if (pc.Verbose)
                {
                    var returnMsg = _strings.GetText(strs.perm_prevent(index + 1,
                        Format.Bold(pc.Permissions[index].GetCommand(_cmd.GetPrefix(sg), sg))));

                    try { await usrMsg.Channel.SendErrorAsync(_eb, returnMsg); }
                    catch { }

                    Log.Information("{PermissionMessage}", returnMsg);
                }

                return true;
            }

            var cleverbotExecuted = await TryAsk(cbs, (ITextChannel)usrMsg.Channel, message);
            if (cleverbotExecuted)
            {
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
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error in cleverbot");
        }

        return false;
    }
}