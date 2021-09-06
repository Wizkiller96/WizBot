using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Impl;
using NadekoBot.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NadekoBot.Common.Replacements;
using NadekoBot.Core.Common;
using Serilog;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility : NadekoModule
    {
        private readonly DiscordSocketClient _client;
        private readonly IStatsService _stats;
        private readonly IBotCredentials _creds;
        private readonly NadekoBot _bot;
        private readonly DownloadTracker _tracker;

        public Utility(NadekoBot nadeko, DiscordSocketClient client,
            IStatsService stats, IBotCredentials creds, DownloadTracker tracker)
        {
            _client = client;
            _stats = stats;
            _creds = creds;
            _bot = nadeko;
            _tracker = tracker;
        }
        

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        [Priority(1)]
        public async Task Say(ITextChannel channel, [Leftover] string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            var rep = new ReplacementBuilder()
                .WithDefault(ctx.User, channel, (SocketGuild)ctx.Guild, (DiscordSocketClient)ctx.Client)
                .Build();

            if (CREmbed.TryParse(message, out var embedData))
            {
                rep.Replace(embedData);
                await channel.EmbedAsync(embedData, sanitizeAll: !((IGuildUser)Context.User).GuildPermissions.MentionEveryone).ConfigureAwait(false);
            }
            else
            {
                var msg = rep.Replace(message);
                if (!string.IsNullOrWhiteSpace(msg))
                {
                    await channel.SendConfirmAsync(msg).ConfigureAwait(false);
                }
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        [Priority(0)]
        public Task Say([Leftover] string message) =>
            Say((ITextChannel)ctx.Channel, message);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task WhosPlaying([Leftover] string game)
        {
            game = game?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(game))
                return;

            if (!(ctx.Guild is SocketGuild socketGuild))
            {
                Log.Warning("Can't cast guild to socket guild.");
                return;
            }
            var rng = new NadekoRandom();
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
        
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public async Task InRole(int page, [Leftover] IRole role = null)
        {
            if (--page < 0)
                return;
            
            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
            await _tracker.EnsureUsersDownloadedAsync(ctx.Guild).ConfigureAwait(false);

            var users = await ctx.Guild.GetUsersAsync();
            var roleUsers = users
                .Where(u => role is null ? u.RoleIds.Count == 1 : u.RoleIds.Contains(role.Id))
                .Select(u => $"`{u.Id, 18}` {u}")
                .ToArray();

            await ctx.SendPaginatedConfirmAsync(page, (cur) =>
            {
                var pageUsers = roleUsers.Skip(cur * 20)
                    .Take(20)
                    .ToList();

                if (pageUsers.Count == 0)
                    return new EmbedBuilder().WithOkColor().WithDescription(GetText("no_user_on_this_page"));
                    
                return new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("inrole_list", Format.Bold(role?.Name ?? "No Role")) + $" - {roleUsers.Length}")
                    .WithDescription(string.Join("\n", pageUsers));
            }, roleUsers.Length, 20).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public Task InRole([Leftover] IRole role = null)
            => InRole(1, role);

        public enum MeOrBot { Me, Bot }

        [NadekoCommand, Usage, Description, Aliases]
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

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task UserId([Leftover] IGuildUser target = null)
        {
            var usr = target ?? ctx.User;
            await ReplyConfirmLocalizedAsync("userid", "🆔", Format.Bold(usr.ToString()),
                Format.Code(usr.Id.ToString())).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task RoleId([Leftover] IRole role)
        {
            await ReplyConfirmLocalizedAsync("roleid", "🆔", Format.Bold(role.ToString()),
                Format.Code(role.Id.ToString())).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task ChannelId()
        {
            await ReplyConfirmLocalizedAsync("channelid", "🆔", Format.Code(ctx.Channel.Id.ToString()))
                .ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ServerId()
        {
            await ReplyConfirmLocalizedAsync("serverid", "🆔", Format.Code(ctx.Guild.Id.ToString()))
                .ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
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

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task Roles(int page = 1) =>
            Roles(null, page);

        [NadekoCommand, Usage, Description, Aliases]
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

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Stats()
        {
            var ownerIds = string.Join("\n", _creds.OwnerIds);
            if (string.IsNullOrWhiteSpace(ownerIds))
                ownerIds = "-";

            await ctx.Channel.EmbedAsync(
                new EmbedBuilder().WithOkColor()
                    .WithAuthor(eab => eab.WithName($"NadekoBot v{StatsService.BotVersion}")
                                          .WithUrl("http://nadekobot.readthedocs.io/en/latest/")
                                          .WithIconUrl("https://nadeko-pictures.nyc3.digitaloceanspaces.com/other/avatar.png"))
                    .AddField(efb => efb.WithName(GetText("author")).WithValue(_stats.Author).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("botid")).WithValue(_client.CurrentUser.Id.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("shard")).WithValue($"#{_client.ShardId} / {_creds.TotalShards}").WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("commands_ran")).WithValue(_stats.CommandsRan.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("messages")).WithValue($"{_stats.MessageCounter} ({_stats.MessagesPerSecond:F2}/sec)").WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("memory")).WithValue($"{_stats.Heap} MB").WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("owner_ids")).WithValue(ownerIds).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("uptime")).WithValue(_stats.GetUptimeString("\n")).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("presence")).WithValue(
                        GetText("presence_txt",
                            _bot.GuildCount, _stats.TextChannels, _stats.VoiceChannels)).WithIsInline(true))).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Showemojis([Leftover] string _) // need to have the parameter so that the message.tags gets populated
        {
            var tags = ctx.Message.Tags.Where(t => t.Type == TagType.Emoji).Select(t => (Emote)t.Value);

            var result = string.Join("\n", tags.Select(m => GetText("showemojis", m, m.Url)));

            if (string.IsNullOrWhiteSpace(result))
                await ReplyErrorLocalizedAsync("showemojis_none").ConfigureAwait(false);
            else
                await ctx.Channel.SendMessageAsync(result.TrimTo(2000)).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [OwnerOnly]
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


        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task SaveChat(int cnt)
        {
            var msgs = new List<IMessage>(cnt);
            await ctx.Channel.GetMessagesAsync(cnt).ForEachAsync(dled => msgs.AddRange(dled)).ConfigureAwait(false);

            var title = $"Chatlog-{ctx.Guild.Name}/#{ctx.Channel.Name}-{DateTime.Now}.txt";
            var grouping = msgs.GroupBy(x => $"{x.CreatedAt.Date:dd.MM.yyyy}")
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

        [NadekoCommand, Usage, Description, Aliases]
#if GLOBAL_NADEKO
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

        public enum CreateInviteType
        {
            Any,
            New
        }
        
        // [NadekoCommand, Usage, Description, Aliases]
        // [RequireContext(ContextType.Guild)]
        // public async Task CreateMyInvite(CreateInviteType type = CreateInviteType.Any)
        // {
        //     if (type == CreateInviteType.Any)
        //     {
        //         if (_inviteService.TryGetInvite(type, out var code))
        //         {
        //             await ReplyConfirmLocalizedAsync("your_invite", $"https://discord.gg/{code}");
        //             return;
        //         }
        //     }
        //     
        //     var invite = await ((ITextChannel) ctx.Channel).CreateInviteAsync(isUnique: true);
        // }
        //
        // [NadekoCommand, Usage, Description, Aliases]
        // [RequireContext(ContextType.Guild)]
        // public async Task InviteLb(int page = 1)
        // {
        //     if (--page < 0)
        //         return;
        //
        //     var inviteUsers = await _inviteService.GetInviteUsersAsync(ctx.Guild.Id);
        //     
        //     var embed = new EmbedBuilder()
        //         .WithOkColor();
        //
        //     await ctx.SendPaginatedConfirmAsync(page, (curPage) =>
        //     {
        //         var items = inviteUsers.Skip(curPage * 9).Take(9);
        //         var i = 0;
        //         foreach (var item in items)
        //             embed.AddField($"#{curPage * 9 + ++i} {item.UserName} [{item.User.Id}]", item.InvitedUsers);
        //
        //         return embed;
        //     }, inviteUsers.Count, 9);
        // }
    }
}
