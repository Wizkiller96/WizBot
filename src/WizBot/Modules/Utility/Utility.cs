#nullable disable
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using WizBot.Modules.Utility.Services;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wiz.Common;
using SystemTextJsonSamples;

namespace WizBot.Modules.Utility;

public partial class Utility : WizBotModule
{
    public enum CreateInviteType
    {
        Any,
        New
    }

    public enum MeOrBot { Me, Bot }

    private static readonly JsonSerializerOptions _showEmbedSerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = LowerCaseNamingPolicy.Default
    };

    private static SemaphoreSlim sem = new(1, 1);
    private readonly DiscordSocketClient _client;
    private readonly ICoordinator _coord;
    private readonly IStatsService _stats;
    private readonly IBotCredentials _creds;
    private readonly DownloadTracker _tracker;
    private readonly IHttpClientFactory _httpFactory;
    private readonly VerboseErrorsService _veService;

    public Utility(
        DiscordSocketClient client,
        ICoordinator coord,
        IStatsService stats,
        IBotCredentials creds,
        DownloadTracker tracker,
        IHttpClientFactory httpFactory,
        VerboseErrorsService veService)
    {
        _client = client;
        _coord = coord;
        _stats = stats;
        _creds = creds;
        _tracker = tracker;
        _httpFactory = httpFactory;
        _veService = veService;
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.ManageMessages)]
    [Priority(1)]
    public async Task Say(ITextChannel channel, [Leftover] SmartText message)
    {
        var rep = new ReplacementBuilder()
                  .WithDefault(ctx.User, channel, (SocketGuild)ctx.Guild, (DiscordSocketClient)ctx.Client)
                  .Build();

        message = rep.Replace(message);

        await channel.SendAsync(message, !((IGuildUser)ctx.User).GuildPermissions.MentionEveryone);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.ManageMessages)]
    [Priority(0)]
    public Task Say([Leftover] SmartText message)
        => Say((ITextChannel)ctx.Channel, message);

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async Task WhosPlaying([Leftover] string game)
    {
        game = game?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(game))
            return;

        if (ctx.Guild is not SocketGuild socketGuild)
        {
            Log.Warning("Can't cast guild to socket guild");
            return;
        }

        var rng = new WizBotRandom();
        var arr = await Task.Run(() => socketGuild.Users
                                                  .Where(u => u.Activities.FirstOrDefault()?.Name?.Trim().ToUpperInvariant()
                                                              == game)
                                                  .Select(u => u.Username)
                                                  .OrderBy(_ => rng.Next())
                                                  .Take(60)
                                                  .ToArray());

        var i = 0;
        if (arr.Length == 0)
            await ReplyErrorLocalizedAsync(strs.nobody_playing_game);
        else
        {
            await SendConfirmAsync("```css\n"
                                   + string.Join("\n",
                                       arr.GroupBy(_ => i++ / 2)
                                          .Select(ig => string.Concat(ig.Select(el => $"• {el,-27}"))))
                                   + "\n```");
        }
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [Priority(0)]
    public async Task InRole(int page, [Leftover] IRole role = null)
    {
        if (--page < 0)
            return;

        await ctx.Channel.TriggerTypingAsync();
        await _tracker.EnsureUsersDownloadedAsync(ctx.Guild);

        var users = await ctx.Guild.GetUsersAsync(
            CacheMode.CacheOnly
        );

        var roleUsers = users.Where(u => role is null ? u.RoleIds.Count == 1 : u.RoleIds.Contains(role.Id))
                             .Select(u => $"`{u.Id,18}` {u}")
                             .ToArray();

        await ctx.SendPaginatedConfirmAsync(page,
            cur =>
            {
                var pageUsers = roleUsers.Skip(cur * 20).Take(20).ToList();

                if (pageUsers.Count == 0)
                    return _eb.Create().WithOkColor().WithDescription(GetText(strs.no_user_on_this_page));

                return _eb.Create()
                          .WithOkColor()
                          .WithTitle(GetText(strs.inrole_list(Format.Bold(role?.Name ?? "No Role"), roleUsers.Length)))
                          .WithDescription(string.Join("\n", pageUsers));
            },
            roleUsers.Length,
            20);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [Priority(1)]
    public Task InRole([Leftover] IRole role = null)
        => InRole(1, role);

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async Task CheckPerms(MeOrBot who = MeOrBot.Me)
    {
        var builder = new StringBuilder();
        var user = who == MeOrBot.Me ? (IGuildUser)ctx.User : ((SocketGuild)ctx.Guild).CurrentUser;
        var perms = user.GetPermissions((ITextChannel)ctx.Channel);
        foreach (var p in perms.GetType()
                               .GetProperties()
                               .Where(static p =>
                               {
                                   var method = p.GetGetMethod();
                                   if (method is null)
                                       return false;
                                   return !method.GetParameters().Any();
                               }))
            builder.AppendLine($"{p.Name} : {p.GetValue(perms, null)}");
        await SendConfirmAsync(builder.ToString());
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async Task UserId([Leftover] IGuildUser target = null)
    {
        var usr = target ?? ctx.User;
        await ReplyConfirmLocalizedAsync(strs.userid("🆔",
            Format.Bold(usr.ToString()),
            Format.Code(usr.Id.ToString())));
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async Task RoleId([Leftover] IRole role)
        => await ReplyConfirmLocalizedAsync(strs.roleid("🆔",
            Format.Bold(role.ToString()),
            Format.Code(role.Id.ToString())));

    [Cmd]
    public async Task ChannelId()
        => await ReplyConfirmLocalizedAsync(strs.channelid("🆔", Format.Code(ctx.Channel.Id.ToString())));

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async Task ServerId()
        => await ReplyConfirmLocalizedAsync(strs.serverid("🆔", Format.Code(ctx.Guild.Id.ToString())));

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async Task Roles(IGuildUser target, int page = 1)
    {
        var guild = ctx.Guild;

        const int rolesPerPage = 20;

        if (page is < 1 or > 100)
            return;

        if (target is not null)
        {
            var roles = target.GetRoles()
                              .Except(new[] { guild.EveryoneRole })
                              .OrderBy(r => -r.Position)
                              .Skip((page - 1) * rolesPerPage)
                              .Take(rolesPerPage)
                              .ToArray();
            if (!roles.Any())
                await ReplyErrorLocalizedAsync(strs.no_roles_on_page);
            else
            {
                await SendConfirmAsync(GetText(strs.roles_page(page, Format.Bold(target.ToString()))),
                    "\n• " + string.Join("\n• ", (IEnumerable<IRole>)roles).SanitizeMentions(true));
            }
        }
        else
        {
            var roles = guild.Roles.Except(new[] { guild.EveryoneRole })
                             .OrderBy(r => -r.Position)
                             .Skip((page - 1) * rolesPerPage)
                             .Take(rolesPerPage)
                             .ToArray();
            if (!roles.Any())
                await ReplyErrorLocalizedAsync(strs.no_roles_on_page);
            else
            {
                await SendConfirmAsync(GetText(strs.roles_all_page(page)),
                    "\n• " + string.Join("\n• ", (IEnumerable<IRole>)roles).SanitizeMentions(true));
            }
        }
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public Task Roles(int page = 1)
        => Roles(null, page);

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async Task ChannelTopic([Leftover] ITextChannel channel = null)
    {
        if (channel is null)
            channel = (ITextChannel)ctx.Channel;

        var topic = channel.Topic;
        if (string.IsNullOrWhiteSpace(topic))
            await ReplyErrorLocalizedAsync(strs.no_topic_set);
        else
            await SendConfirmAsync(GetText(strs.channel_topic), topic);
    }

    [Cmd]
    public async Task Stats()
    {
        var ownerIds = string.Join("\n", _creds.OwnerIds);
        if (string.IsNullOrWhiteSpace(ownerIds))
            ownerIds = "-";

        var adminIds = string.Join("\n", _creds.AdminIds);
        if (string.IsNullOrWhiteSpace(adminIds))
            adminIds = "-";

        await ctx.Channel.EmbedAsync(_eb.Create()
                                        .WithOkColor()
                                        .WithAuthor($"WizBot v{StatsService.BOT_VERSION}",
                                            "https://cdn.wizbot.cc/other/bot/wizbot_icon.png",
                                            "https://wizbot.readthedocs.io/en/latest/")
                                        .WithImageUrl("https://cdn.wizbot.cc/other/bot/wizbot-banner.jpg")
                                        .AddField(GetText(strs.author), _stats.Author, true)
                                        .AddField(GetText(strs.library), _stats.Library, true)
                                        .AddField(GetText(strs.botid), _client.CurrentUser.Id.ToString(), true)
                                        .AddField(GetText(strs.shard),
                                            $"#{_client.ShardId} / {_creds.TotalShards}",
                                            true)
                                        .AddField(GetText(strs.commands_ran), _stats.CommandsRan.ToString(), true)
                                        .AddField(GetText(strs.messages),
                                            $"{_stats.MessageCounter} ({_stats.MessagesPerSecond:F2}/sec)",
                                            true)
                                        .AddField(GetText(strs.memory),
                                            FormattableString.Invariant($"{_stats.GetPrivateMemoryMegabytes():F2} MB"),
                                            true)
                                        .AddField(GetText(strs.owner_ids), ownerIds, true)
                                        .AddField(GetText(strs.admin_ids), adminIds, true)
                                        .AddField(GetText(strs.uptime), _stats.GetUptimeString("\n"), true)
                                        .AddField(GetText(strs.presence),
                                            GetText(strs.presence_txt(_coord.GetGuildCount(),
                                                _stats.TextChannels,
                                                _stats.VoiceChannels)),
                                            true));
    }

    [Cmd]
    public async Task
        Showemojis([Leftover] string _) // need to have the parameter so that the message.tags gets populated
    {
        var tags = ctx.Message.Tags.Where(t => t.Type == TagType.Emoji).Select(t => (Emote)t.Value);

        var result = string.Join("\n", tags.Select(m => GetText(strs.showemojis(m, m.Url))));

        if (string.IsNullOrWhiteSpace(result))
            await ReplyErrorLocalizedAsync(strs.showemojis_none);
        else
            await ctx.Channel.SendMessageAsync(result.TrimTo(2000));
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [BotPerm(GuildPerm.ManageEmojisAndStickers)]
    [UserPerm(GuildPerm.ManageEmojisAndStickers)]
    [Priority(2)]
    public Task EmojiAdd(string name, Emote emote)
        => EmojiAdd(name, emote.Url);

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [BotPerm(GuildPerm.ManageEmojisAndStickers)]
    [UserPerm(GuildPerm.ManageEmojisAndStickers)]
    [Priority(1)]
    public Task EmojiAdd(Emote emote)
        => EmojiAdd(emote.Name, emote.Url);

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [BotPerm(GuildPerm.ManageEmojisAndStickers)]
    [UserPerm(GuildPerm.ManageEmojisAndStickers)]
    [Priority(0)]
    public async Task EmojiAdd(string name, string url = null)
    {
        name = name.Trim(':');

        url ??= ctx.Message.Attachments.FirstOrDefault()?.Url;

        if (url is null)
            return;

        using var http = _httpFactory.CreateClient();
        using var res = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        if (!res.IsImage() || res.GetContentLength() > 262_144)
        {
            await ReplyErrorLocalizedAsync(strs.invalid_emoji_link);
            return;
        }

        await using var imgStream = await res.Content.ReadAsStreamAsync();
        Emote em;
        try
        {
            em = await ctx.Guild.CreateEmoteAsync(name, new(imgStream));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error adding emoji on server {GuildId}", ctx.Guild.Id);

            await ReplyErrorLocalizedAsync(strs.emoji_add_error);
            return;
        }

        await ConfirmLocalizedAsync(strs.emoji_added(em.ToString()));
    }
    
    [Cmd]
    [RequireContext(ContextType.Guild)]
    [BotPerm(GuildPerm.ManageEmojisAndStickers)]
    [UserPerm(GuildPerm.ManageEmojisAndStickers)]
    [Priority(0)]
    public async Task EmojiRemove(params Emote[] emotes)
    {
        if (emotes.Length == 0)
            return;
        
        var g = (SocketGuild)ctx.Guild;

        var fails = new List<Emote>();
        foreach (var emote in emotes)
        {
            var guildEmote = g.Emotes.FirstOrDefault(x => x.Id == emote.Id);
            if (guildEmote is null)
            {
                fails.Add(emote);
            }
            else
            {
                await ctx.Guild.DeleteEmoteAsync(guildEmote);
            }
        }

        if (fails.Count > 0)
        {
            await ReplyPendingLocalizedAsync(strs.emoji_not_removed(fails.Select(x => x.ToString()).Join(" ")));
            return;
        }

        await ctx.OkAsync();
    }
    
    [Cmd]
    [RequireContext(ContextType.Guild)]
    [BotPerm(GuildPerm.ManageEmojisAndStickers)]
    [UserPerm(GuildPerm.ManageEmojisAndStickers)]
    public async Task StickerAdd(string name = null, string description = null, params string[] tags)
    {
        string format;
        Stream stream;

        if (ctx.Message.Stickers.Count is 1 && ctx.Message.Stickers.First() is SocketSticker ss)
        {
            name ??= ss.Name;
            description = ss.Description;
            tags = tags is null or { Length: 0 } ? ss.Tags.ToArray() : tags;
            format = FormatToExtension(ss.Format);

            using var http = _httpFactory.CreateClient();
            stream = await http.GetStreamAsync(ss.GetStickerUrl());
        }
        // else if (ctx.Message.Attachments.FirstOrDefault() is { } attachment)
        // {
        //     var url = attachment?.Url;
        //
        //     if (url is null)
        //         return;
        //
        //     if (name is null)
        //     {
        //         await ReplyErrorLocalizedAsync(strs.sticker_missing_name);
        //         return;
        //     }
        //
        //     format = Path.GetExtension(attachment.Filename);
        //
        //     if (attachment is not { Width: 300, Height: 300 })
        //     {
        //         await ReplyErrorLocalizedAsync(strs.sticker_invalid_size);
        //         return;
        //     }
        //
        //     using var http = _httpFactory.CreateClient();
        //     
        //     using var res = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        //     if (res.GetContentLength() > 512.Kilobytes().Bytes)
        //     {
        //         await ReplyErrorLocalizedAsync(strs.invalid_emoji_link);
        //         return;
        //     }
        //
        //     stream = await res.Content.ReadAsStreamAsync();
        // }
        else
        {
            await ReplyErrorLocalizedAsync(strs.sticker_error);
            return;
        }
        
        try
        {
            if (tags.Length == 0)
                tags = new[] { name };

            await ctx.Guild.CreateStickerAsync(name,
                string.IsNullOrWhiteSpace(description) ? "Missing description" : description,
                tags,
                stream,
                $"{name}.{format}");

            await ctx.OkAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error occurred while adding a sticker: {Message}", ex.Message);
            await ReplyErrorLocalizedAsync(strs.error_occured);
        }
        finally
        {
            await stream.DisposeAsync();
        }
    }
    
    private static string FormatToExtension(StickerFormatType format)
    {
      switch (format)
      {
        case StickerFormatType.None:
        case StickerFormatType.Png:
        case StickerFormatType.Apng:
          return "png";
        case StickerFormatType.Lottie:
          return "lottie";
        default:
          throw new ArgumentException(nameof (format));
      }
    }

    [Cmd]
    [OwnerOnly]
    public async Task ListServers(int page = 1)
    {
        page -= 1;

        if (page < 0)
            return;

        var guilds = _client.Guilds.OrderBy(g => g.Name)
                            .Skip(page * 15)
                            .Take(15)
                            .ToList();

        if (!guilds.Any())
        {
            await ReplyErrorLocalizedAsync(strs.listservers_none);
            return;
        }

        var embed = _eb.Create().WithOkColor();
        foreach (var guild in guilds)
            embed.AddField(guild.Name, GetText(strs.listservers(guild.Id, guild.MemberCount, guild.OwnerId)));

        await ctx.Channel.EmbedAsync(embed);
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public Task ShowEmbed(ulong messageId)
        => ShowEmbed((ITextChannel)ctx.Channel, messageId);

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async Task ShowEmbed(ITextChannel ch, ulong messageId)
    {
        var user = (IGuildUser)ctx.User;
        var perms = user.GetPermissions(ch);
        if (!perms.ReadMessageHistory || !perms.ViewChannel)
        {
            await ReplyErrorLocalizedAsync(strs.insuf_perms_u);
            return;
        }

        var msg = await ch.GetMessageAsync(messageId);
        if (msg is null)
        {
            await ReplyErrorLocalizedAsync(strs.msg_not_found);
            return;
        }

        if (!msg.Embeds.Any())
        {
            await ReplyErrorLocalizedAsync(strs.not_found);
            return;
        }

        var json = new SmartEmbedTextArray()
        {
            Content = msg.Content,
            Embeds = msg.Embeds
                        .Map(x => new SmartEmbedArrayElementText(x))
        }.ToJson(_showEmbedSerializerOptions);
        
        await SendConfirmAsync(Format.Code(json, "json").Replace("](", "]\\("));
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [AdminOnly]
    public async Task SaveChat(int cnt)
    {
        var msgs = new List<IMessage>(cnt);
        await ctx.Channel.GetMessagesAsync(cnt).ForEachAsync(dled => msgs.AddRange(dled));

        var title = $"Chatlog-{ctx.Guild.Name}/#{ctx.Channel.Name}-{DateTime.Now}.txt";
        var grouping = msgs.GroupBy(x => $"{x.CreatedAt.Date:dd.MM.yyyy}")
                           .Select(g => new
                           {
                               date = g.Key,
                               messages = g.OrderBy(x => x.CreatedAt)
                                           .Select(s =>
                                           {
                                               var msg = $"【{s.Timestamp:HH:mm:ss}】{s.Author}:";
                                               if (string.IsNullOrWhiteSpace(s.ToString()))
                                               {
                                                   if (s.Attachments.Any())
                                                   {
                                                       msg += "FILES_UPLOADED: "
                                                              + string.Join("\n", s.Attachments.Select(x => x.Url));
                                                   }
                                                   else if (s.Embeds.Any())
                                                   {
                                                       msg += "EMBEDS: "
                                                              + string.Join("\n--------\n",
                                                                  s.Embeds.Select(x
                                                                      => $"Description: {x.Description}"));
                                                   }
                                               }
                                               else
                                                   msg += s.ToString();

                                               return msg;
                                           })
                           });
        await using var stream = await JsonConvert.SerializeObject(grouping, Formatting.Indented).ToStream();
        await ctx.User.SendFileAsync(stream, title, title);
    }

    [Cmd]
    public async Task Ping()
    {
        await sem.WaitAsync(5000);
        try
        {
            var sw = Stopwatch.StartNew();
            var msg = await ctx.Channel.SendMessageAsync("🏓");
            sw.Stop();
            msg.DeleteAfter(0);

            await SendConfirmAsync($"{Format.Bold(ctx.User.ToString())} 🏓 {(int)sw.Elapsed.TotalMilliseconds}ms");
        }
        finally
        {
            sem.Release();
        }
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.ManageMessages)]
    public async Task VerboseError(bool? newstate = null)
    {
        var state = _veService.ToggleVerboseErrors(ctx.Guild.Id, newstate);

        if (state)
            await ReplyConfirmLocalizedAsync(strs.verbose_errors_enabled);
        else
            await ReplyConfirmLocalizedAsync(strs.verbose_errors_disabled);
    }

    [Cmd]
    [OnlyPublicBot]
    public async Task Donators()
    {
        // Make it so it wont error when no users are found.
            var dusers = _client.GetGuild(99273784988557312).GetRole(280182841114099722).Members;
            var pusers = _client.GetGuild(99273784988557312).GetRole(299174013597646868).Members;

            await ctx.Channel.EmbedAsync(_eb.Create().WithOkColor()
                .WithTitle($"WizBot - Donators")
                .WithDescription("List of users who have donated to WizBot.")
                .AddField("Donators:", string.Join("\n", dusers), false))
                .ConfigureAwait(false);

            await ctx.Channel.EmbedAsync(_eb.Create().WithOkColor()
                .WithTitle($"WizBot - Patreon Donators")
                .WithDescription("List of users who have donated through WizNet's Patreon.")
                .AddField("Patreon Donators:", string.Join("\n", pusers), false))
                .ConfigureAwait(false);
    }

    [Cmd]
    [OnlyPublicBot]
    public async Task WizNet()
    {
        // Make it so it wont error when no users are found.
            var wnstaff = _client.GetGuild(99273784988557312).GetRole(348560594045108245).Members; // WizNet Staff
            var wbstaff = _client.GetGuild(99273784988557312).GetRole(367646195889471499).Members; // WizBot Staff

            await ctx.Channel.EmbedAsync(_eb.Create().WithOkColor()
                .WithTitle("WizNet's Info")
                .WithThumbnailUrl("https://i.imgur.com/Go5ZymW.png")
                .WithDescription("WizNet is a small internet company that was made by Wizkiller96. The site first started off more as a social platform for his friends to have a place to hangout and chat with each other and share their work. Since then the site has gone through many changes and reforms. It now sits as a small hub for all the services and work WizNet provides to the public.")
                .AddField("Websites", "[WizNet](http://wiznet.work/)\n[Wiz VPS](http://wiz-vps.com/)\n[WizBot](http://wizbot.cc)", true)
                .AddField("Social Media", "[Facebook](http://facebook.com/Wizkiller96Network)\n[WizBot's Twitter](http://twitter.com/WizBot_Dev)", true)
                .AddField("WizNet Staff", string.Join("\n", wnstaff), false)
                .AddField("WizBot Staff", string.Join("\n", wbstaff), false)
                .WithFooter("Note: Not all staff are listed here.")).ConfigureAwait(false);
    }
}