using Discord;
using Discord.Commands;
using Discord.WebSocket;
using WizBot.Common;
using WizBot.Common.Attributes;
using WizBot.Core.Services;
using WizBot.Core.Services.Impl;
using WizBot.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WizBot.Core.Common;
using WizBot.Core.Common.Attributes;

namespace WizBot.Modules.Utility
{
    public partial class Utility : WizBotTopLevelModule
    {
        private readonly DiscordSocketClient _client;
        private readonly IStatsService _stats;
        private readonly IBotCredentials _creds;
        private readonly WizBot _bot;
        private readonly DbService _db;
        private readonly IHttpClientFactory _httpFactory;
        private readonly DownloadTracker _tracker;

        public Utility(WizBot wizbot, DiscordSocketClient client,
            IStatsService stats, IBotCredentials creds,
            DbService db, IHttpClientFactory factory, DownloadTracker tracker)
        {
            _client = client;
            _stats = stats;
            _creds = creds;
            _bot = wizbot;
            _db = db;
            _httpFactory = factory;
            _tracker = tracker;
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task TogetherTube()
        {
            Uri target;
            using (var http = _httpFactory.CreateClient())
            using (var res = await http.GetAsync("https://togethertube.com/room/create").ConfigureAwait(false))
            {
                target = res.RequestMessage.RequestUri;
            }

            await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                .WithAuthor(eab => eab.WithIconUrl("https://togethertube.com/assets/img/favicons/favicon-32x32.png")
                .WithName("Together Tube")
                .WithUrl("https://togethertube.com/"))
                .WithDescription(ctx.User.Mention + " " + GetText("togtub_room_link") + "\n" + target)).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task WhosPlaying([Leftover] string game)
        {
            game = game?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(game))
                return;

            if (!(ctx.Guild is SocketGuild socketGuild))
            {
                _log.Warn("Can't cast guild to socket guild.");
                return;
            }
            var rng = new WizBotRandom();
            var arr = await Task.Run(() => socketGuild.Users
                    .Where(u => u.Activity?.Name?.ToUpperInvariant() == game)
                    .Select(u => u.Username)
                    .OrderBy(x => rng.Next())
                    .Take(60)
                    .ToArray()).ConfigureAwait(false);

            int i = 0;
            if (arr.Length == 0)
                await ReplyErrorLocalizedAsync("nobody_playing_game").ConfigureAwait(false);
            else
            {
                await ctx.Channel.SendConfirmAsync("```css\n" + string.Join("\n", arr.GroupBy(item => (i++) / 2)
                                                                                 .Select(ig => string.Concat(ig.Select(el => $"• {el,-27}")))) + "\n```")
                                                                                 .ConfigureAwait(false);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task InRole([Leftover] IRole role)
        {
            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
            await _tracker.EnsureUsersDownloadedAsync(ctx.Guild).ConfigureAwait(false);

            var users = await ctx.Guild.GetUsersAsync();
            var roleUsers = users
                .Where(u => u.RoleIds.Contains(role.Id))
                .Select(u => u.ToString())
                .ToArray();

            await ctx.SendPaginatedConfirmAsync(0, (cur) =>
            {
                return new EmbedBuilder().WithOkColor()
                    .WithTitle(Format.Bold(GetText("inrole_list", Format.Bold(role.Name))) + $" - {roleUsers.Length}")
                    .WithDescription(string.Join("\n", roleUsers.Skip(cur * 20).Take(20)));
            }, roleUsers.Length, 20).ConfigureAwait(false);
        }

        public enum MeOrBot { Me, Bot }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task CheckPerms(MeOrBot who = MeOrBot.Me)
        {
            StringBuilder builder = new StringBuilder();
            var user = who == MeOrBot.Me
                ? (IGuildUser)ctx.User
                : ((SocketGuild)ctx.Guild).CurrentUser;
            var perms = user.GetPermissions((ITextChannel)ctx.Channel);
            foreach (var p in perms.GetType().GetProperties().Where(p => !p.GetGetMethod().GetParameters().Any()))
            {
                builder.AppendLine($"{p.Name} : {p.GetValue(perms, null)}");
            }
            await ctx.Channel.SendConfirmAsync(builder.ToString()).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task UserId([Leftover] IGuildUser target = null)
        {
            var usr = target ?? ctx.User;
            await ReplyConfirmLocalizedAsync("userid", "🆔", Format.Bold(usr.ToString()),
                Format.Code(usr.Id.ToString())).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task RoleId([Leftover] IRole role)
        {
            await ReplyConfirmLocalizedAsync("roleid", "🆔", Format.Bold(role.ToString()),
                Format.Code(role.Id.ToString())).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task ChannelId()
        {
            await ReplyConfirmLocalizedAsync("channelid", "🆔", Format.Code(ctx.Channel.Id.ToString()))
                .ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ServerId()
        {
            await ReplyConfirmLocalizedAsync("serverid", "🆔", Format.Code(ctx.Guild.Id.ToString()))
                .ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Roles(IGuildUser target, int page = 1)
        {
            var channel = (ITextChannel)ctx.Channel;
            var guild = channel.Guild;

            const int rolesPerPage = 20;

            if (page < 1 || page > 100)
                return;

            if (target != null)
            {
                var roles = target.GetRoles().Except(new[] { guild.EveryoneRole }).OrderBy(r => -r.Position).Skip((page - 1) * rolesPerPage).Take(rolesPerPage).ToArray();
                if (!roles.Any())
                {
                    await ReplyErrorLocalizedAsync("no_roles_on_page").ConfigureAwait(false);
                }
                else
                {

                    await channel.SendConfirmAsync(GetText("roles_page", page, Format.Bold(target.ToString())),
                        "\n• " + string.Join("\n• ", (IEnumerable<IRole>)roles).SanitizeMentions(true)).ConfigureAwait(false);
                }
            }
            else
            {
                var roles = guild.Roles.Except(new[] { guild.EveryoneRole }).OrderBy(r => -r.Position).Skip((page - 1) * rolesPerPage).Take(rolesPerPage).ToArray();
                if (!roles.Any())
                {
                    await ReplyErrorLocalizedAsync("no_roles_on_page").ConfigureAwait(false);
                }
                else
                {
                    await channel.SendConfirmAsync(GetText("roles_all_page", page),
                        "\n• " + string.Join("\n• ", (IEnumerable<IRole>)roles).SanitizeMentions(true)).ConfigureAwait(false);
                }
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task Roles(int page = 1) =>
            Roles(null, page);

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ChannelTopic([Leftover]ITextChannel channel = null)
        {
            if (channel == null)
                channel = (ITextChannel)ctx.Channel;

            var topic = channel.Topic;
            if (string.IsNullOrWhiteSpace(topic))
                await ReplyErrorLocalizedAsync("no_topic_set").ConfigureAwait(false);
            else
                await ctx.Channel.SendConfirmAsync(GetText("channel_topic"), topic).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task Stats()
        {
            var sw = Stopwatch.StartNew();
            var msg = await ctx.Channel.SendMessageAsync("Getting Ping...").ConfigureAwait(false);
            sw.Stop();
            msg.DeleteAfter(0);

            var ownerIds = string.Join("\n", _creds.OwnerIds);
            if (string.IsNullOrWhiteSpace(ownerIds))
                ownerIds = "-";

            var adminIds = string.Join("\n", _creds.AdminIds);
            if (string.IsNullOrWhiteSpace(adminIds))
                adminIds = "-";

            await ctx.Channel.EmbedAsync(
                new EmbedBuilder().WithOkColor()
                    .WithAuthor(eab => eab.WithName($"WizBot v{StatsService.BotVersion}")
                                          .WithUrl("http://docs.wizbot.cc/")
                                          .WithIconUrl("http://i.imgur.com/fObUYFS.jpg"))
                                          .WithImageUrl("https://i.imgur.com/hT2UCqu.jpg")
                    .AddField(efb => efb.WithName(GetText("author")).WithValue(_stats.Author).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("library")).WithValue(_stats.Library).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("botid")).WithValue($"🤖 {_client.CurrentUser.Id.ToString()}").WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("shard")).WithValue($"🔷 #{_client.ShardId} / {_creds.TotalShards}").WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("documentation")).WithValue(GetText("documentation_text")).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("commands_ran")).WithValue($"🔣 {_stats.CommandsRan.ToString()}").WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("messages")).WithValue($"💬 {_stats.MessageCounter} ({_stats.MessagesPerSecond:F2}/sec)").WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("memory")).WithValue($"💻 {_stats.Heap} MB").WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("latency")).WithValue($"📡 {(int)sw.Elapsed.TotalMilliseconds} ms").WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("owner_ids")).WithValue(ownerIds).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("admin_ids")).WithValue(adminIds).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("uptime")).WithValue(_stats.GetUptimeString("\n")).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("presence")).WithValue(
                        GetText("presence_txt",
                            _bot.GuildCount, _stats.TextChannels, _stats.VoiceChannels)).WithIsInline(true))).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task Showemojis([Leftover] string _) // need to have the parameter so that the message.tags gets populated
        {
            var tags = ctx.Message.Tags.Where(t => t.Type == TagType.Emoji).Select(t => (Emote)t.Value);

            var result = string.Join("\n", tags.Select(m => GetText("showemojis", m, m.Url)));

            if (string.IsNullOrWhiteSpace(result))
                await ReplyErrorLocalizedAsync("showemojis_none").ConfigureAwait(false);
            else
                await ctx.Channel.SendMessageAsync(result.TrimTo(2000)).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [AdminOnly]
        public async Task ListServers(int page = 1)
        {
            page -= 1;

            if (page < 0)
                return;

            var guilds = await Task.Run(() => _client.Guilds.OrderBy(g => g.Name).Skip((page) * 15).Take(15)).ConfigureAwait(false);

            if (!guilds.Any())
            {
                await ReplyErrorLocalizedAsync("listservers_none").ConfigureAwait(false);
                return;
            }

            await ctx.Channel.EmbedAsync(guilds.Aggregate(new EmbedBuilder().WithOkColor(),
                                     (embed, g) => embed.AddField(efb => efb.WithName(g.Name)
                                                                           .WithValue(
                                             GetText("listservers", g.Id, g.MemberCount,
                                                 g.OwnerId))
                                                                           .WithIsInline(false))))
                         .ConfigureAwait(false);
        }


        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [AdminOnly]
        public async Task SaveChat(int cnt)
        {
            var msgs = new List<IMessage>(cnt);
            await ctx.Channel.GetMessagesAsync(cnt).ForEachAsync(dled => msgs.AddRange(dled)).ConfigureAwait(false);

            var title = $"Chatlog-{ctx.Guild.Name}/#{ctx.Channel.Name}-{DateTime.Now}.txt";
            var grouping = msgs.GroupBy(x => $"{x.CreatedAt.Date:MM.dd.yyyy}")
                .Select(g => new
                {
                    date = g.Key,
                    messages = g.OrderBy(x => x.CreatedAt).Select(s =>
                    {
                        var msg = $"【{s.Timestamp:HH:mm:ss}】{s.Author}:";
                        if (string.IsNullOrWhiteSpace(s.ToString()))
                        {
                            if (s.Attachments.Any())
                            {
                                msg += "FILES_UPLOADED: " + string.Join("\n", s.Attachments.Select(x => x.Url));
                            }
                            else if (s.Embeds.Any())
                            {
                                msg += "EMBEDS: " + string.Join("\n--------\n", s.Embeds.Select(x => $"Description: {x.Description}"));
                            }
                        }
                        else
                        {
                            msg += s.ToString();
                        }
                        return msg;
                    })
                });
            using (var stream = await JsonConvert.SerializeObject(grouping, Formatting.Indented).ToStream().ConfigureAwait(false))
            {
                await ctx.User.SendFileAsync(stream, title, title, false).ConfigureAwait(false);
            }
        }
        private static SemaphoreSlim sem = new SemaphoreSlim(1, 1);

        [WizBotCommand, Usage, Description, Aliases]
#if GLOBAL_WIZBOT
        [Ratelimit(30)]
#endif
        public async Task Ping()
        {
            await sem.WaitAsync(5000).ConfigureAwait(false);
            try
            {
                var sw = Stopwatch.StartNew();
                var msg = await ctx.Channel.SendMessageAsync("🏓").ConfigureAwait(false);
                sw.Stop();
                msg.DeleteAfter(0);

                await ctx.Channel.SendConfirmAsync($"{Format.Bold(ctx.User.ToString())} 🏓 {(int)sw.Elapsed.TotalMilliseconds}ms").ConfigureAwait(false);
            }
            finally
            {
                sem.Release();
            }
        }

        // Old Update Command

        /* [WizBotCommand, Usage, Description, Aliases]
        public async Task Updates()
        {
            await ctx.Channel.EmbedAsync(
                new EmbedBuilder().WithOkColor()
                    .WithAuthor(eab => eab.WithName(GetText($"changelog_title_date"))
                                          .WithUrl("https://github.com/Wizkiller96/WizBot/commits/dev")
                                          .WithIconUrl("http://i.imgur.com/fObUYFS.jpg"))
                    .AddField(efb => efb.WithName(Format.Bold(GetText("changelog_fixes"))).WithValue(GetText("changelog_fixes_msg")).WithIsInline(false))
                    .AddField(efb => efb.WithName(Format.Bold(GetText("changelog_additions"))).WithValue(GetText("changelog_additions_msg")).WithIsInline(false))
                    .AddField(efb => efb.WithName(Format.Bold(GetText("changelog_removals"))).WithValue(GetText("changelog_removals_msg")).WithIsInline(false))
                    .WithFooter(efb => efb.WithText(GetText($"changelog_footer")))
                    );
        } */

        // New Update Command (W.I.P.)

        [WizBotCommand, Usage, Description, Aliases]
        public async Task Updates()
        {
            try
            {
                JToken obj;
                using (var http = _httpFactory.CreateClient())
                {
                    http.DefaultRequestHeaders.Add("User-Agent", "WizBot");
                    obj = JArray.Parse(await http.GetStringAsync($"https://api.github.com/repos/Wizkiller96/WizBot/commits").ConfigureAwait(false));
                }

                // Only temp solution for now as I had no time to clean up the mess.

                var newCommits = (
                    $"[" + $"{obj[0]["sha"]}".TrimTo(6, true) + $"]({obj[0]["html_url"]})" + $" {obj[0]["commit"]["message"]}".TrimTo(50) + $"\n- {obj[0]["author"]["login"]}" + " | " + $"`{obj[0]["commit"]["author"]["date"].ToString()}`" + "\n\n" +
                    $"[" + $"{obj[1]["sha"]}".TrimTo(6, true) + $"]({obj[1]["html_url"]})" + $" {obj[1]["commit"]["message"]}".TrimTo(50) + $"\n- {obj[1]["author"]["login"]}" + " | " + $"`{obj[1]["commit"]["author"]["date"].ToString()}`" + "\n\n" +
                    $"[" + $"{obj[2]["sha"]}".TrimTo(6, true) + $"]({obj[2]["html_url"]})" + $" {obj[2]["commit"]["message"]}".TrimTo(50) + $"\n- {obj[2]["author"]["login"]}" + " | " + $"`{obj[2]["commit"]["author"]["date"].ToString()}`" + "\n\n" +
                    $"[" + $"{obj[3]["sha"]}".TrimTo(6, true) + $"]({obj[3]["html_url"]})" + $" {obj[3]["commit"]["message"]}".TrimTo(50) + $"\n- {obj[3]["author"]["login"]}" + " | " + $"`{obj[3]["commit"]["author"]["date"].ToString()}`" + "\n\n" +
                    $"[" + $"{obj[4]["sha"]}".TrimTo(6, true) + $"]({obj[4]["html_url"]})" + $" {obj[4]["commit"]["message"]}".TrimTo(50) + $"\n- {obj[4]["author"]["login"]}" + " | " + $"`{obj[4]["commit"]["author"]["date"].ToString()}`" + "\n\n" +
                    $"[" + $"{obj[5]["sha"]}".TrimTo(6, true) + $"]({obj[5]["html_url"]})" + $" {obj[5]["commit"]["message"]}".TrimTo(50) + $"\n- {obj[5]["author"]["login"]}" + " | " + $"`{obj[5]["commit"]["author"]["date"].ToString()}`" + "\n\n" +
                    $"[" + $"{obj[6]["sha"]}".TrimTo(6, true) + $"]({obj[6]["html_url"]})" + $" {obj[6]["commit"]["message"]}".TrimTo(50) + $"\n- {obj[6]["author"]["login"]}" + " | " + $"`{obj[6]["commit"]["author"]["date"].ToString()}`" + "\n\n" +
                    $"[" + $"{obj[7]["sha"]}".TrimTo(6, true) + $"]({obj[7]["html_url"]})" + $" {obj[7]["commit"]["message"]}".TrimTo(50) + $"\n- {obj[7]["author"]["login"]}" + " | " + $"`{obj[7]["commit"]["author"]["date"].ToString()}`" + "\n\n" +
                    $"[" + $"{obj[8]["sha"]}".TrimTo(6, true) + $"]({obj[8]["html_url"]})" + $" {obj[8]["commit"]["message"]}".TrimTo(50) + $"\n- {obj[8]["author"]["login"]}" + " | " + $"`{obj[8]["commit"]["author"]["date"].ToString()}`" + "\n\n" +
                    $"[" + $"{obj[9]["sha"]}".TrimTo(6, true) + $"]({obj[9]["html_url"]})" + $" {obj[9]["commit"]["message"]}".TrimTo(50) + $"\n- {obj[9]["author"]["login"]}" + " | " + $"`{obj[9]["commit"]["author"]["date"].ToString()}`"
                );

                await ctx.Channel.EmbedAsync(
                new EmbedBuilder().WithOkColor()
                    .WithAuthor(eab => eab.WithName("WizBot - Latest 10 commits")
                                          .WithUrl("https://github.com/Wizkiller96/WizBot/commits/1.9")
                                          .WithIconUrl("http://i.imgur.com/fObUYFS.jpg"))
                    .WithDescription(newCommits)
                    .WithFooter(efb => efb.WithText(GetText($"changelog_footer")))
                    );
            }
            catch (Exception ex)
            {
                await ctx.Channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }
        [WizBotCommand, Usage, Description, Aliases]
        public async Task Donators()
        {

#if GLOBAL_WIZBOT

            // Make it so it wont error when no users are found.
            var dusers = _client.GetGuild(99273784988557312).GetRole(280182841114099722).Members;
            var pusers = _client.GetGuild(99273784988557312).GetRole(299174013597646868).Members;

            await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                .WithTitle($"WizBot - Donators")
                .WithDescription("List of users who have donated to WizBot.")
                .AddField(fb => fb.WithName("Donators:").WithValue(string.Join("\n", dusers)))).ConfigureAwait(false);

            await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                .WithTitle($"WizBot - Patreon Donators")
                .WithDescription("List of users who have donated through WizNet's Patreon.")
                .AddField(fb => fb.WithName("Patreon Donators:").WithValue(string.Join("\n", pusers)))).ConfigureAwait(false);
#else
            await ctx.Channel.EmbedAsync(new EmbedBuilder().WithErrorColor()
            .WithTitle($"WizBot - Donators")
            .WithDescription("This command is disabled on self-host bots.")).ConfigureAwait(false);
#endif
        }

#if GLOBAL_WIZBOT

        [WizBotCommand, Usage, Description, Aliases]
        public async Task WizNet()
        {

            // Make it so it wont error when no users are found.
            var wnstaff = _client.GetGuild(99273784988557312).GetRole(348560594045108245).Members; // WizNet Staff
            var wbstaff = _client.GetGuild(99273784988557312).GetRole(367646195889471499).Members; // WizBot Staff

            await ctx.Channel.EmbedAsync(new EmbedBuilder().WithColor(431075)
                .WithTitle("WizNet's Info")
                .WithThumbnailUrl("https://i.imgur.com/Go5ZymW.png")
                .WithDescription("WizNet is a small internet company that was made by Wizkiller96. The site first started off more as a social platform for his friends to have a place to hangout and chat with each other and share their work. Since then the site has gone through many changes and reforms. It now sits as a small hub for all the services and work WizNet provides to the public.")
                .AddField(fb => fb.WithName("Websites").WithValue("[WizNet](http://wiznet.ga/)\n[Wiz VPS](http://wiz-vps.com/)\n[WizBot](http://wizbot.cc)").WithIsInline(true))
                .AddField(fb => fb.WithName("Social Media").WithValue("[Facebook](http://facebook.com/Wizkiller96Network)\n[WizBot's Twitter](http://twitter.com/WizBot_Dev)").WithIsInline(true))
                .AddField(fb => fb.WithName("WizNet Staff").WithValue(string.Join("\n", wnstaff)).WithIsInline(false))
                .AddField(fb => fb.WithName("WizBot Staff").WithValue(string.Join("\n", wbstaff)).WithIsInline(false))
                .WithFooter("Note: Not all staff are listed here.")).ConfigureAwait(false);

        }

#endif
    }
}