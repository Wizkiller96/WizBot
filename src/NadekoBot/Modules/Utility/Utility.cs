using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
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
using NadekoBot.Services;
using Serilog;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility : NadekoModule
    {
        private readonly DiscordSocketClient _client;
        private readonly ICoordinator _coord;
        private readonly IStatsService _stats;
        private readonly IBotCredentials _creds;
        private readonly DownloadTracker _tracker;

        public Utility(DiscordSocketClient client, ICoordinator coord,
            IStatsService stats, IBotCredentials creds, DownloadTracker tracker)
        {
            _client = client;
            _coord = coord;
            _stats = stats;
            _creds = creds;
            _tracker = tracker;
        }

        [NadekoCommand, Aliases]
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

        [NadekoCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        [Priority(0)]
        public Task Say([Leftover] SmartText message)
            => Say((ITextChannel)ctx.Channel, message);

        [NadekoCommand, Aliases]
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
                await ReplyErrorLocalizedAsync(strs.nobody_playing_game).ConfigureAwait(false);
            else
            {
                await SendConfirmAsync("```css\n" + string.Join("\n", arr.GroupBy(item => (i++) / 2)
                                                                                 .Select(ig => string.Concat(ig.Select(el => $"• {el,-27}")))) + "\n```")
                                                                                 .ConfigureAwait(false);
            }
        }
        
        [NadekoCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public async Task InRole(int page, [Leftover] IRole role = null)
        {
            if (--page < 0)
                return;
            
            await ctx.Channel.TriggerTypingAsync().ConfigureAwait(false);
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
                    return _eb.Create().WithOkColor().WithDescription(GetText(strs.no_user_on_this_page));
                    
                return _eb.Create().WithOkColor()
                    .WithTitle(GetText(strs.inrole_list(Format.Bold(role?.Name ?? "No Role") + $" - {roleUsers.Length}")))
                    .WithDescription(string.Join("\n", pageUsers));
            }, roleUsers.Length, 20).ConfigureAwait(false);
        }

        [NadekoCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public Task InRole([Leftover] IRole role = null)
            => InRole(1, role);

        public enum MeOrBot { Me, Bot }

        [NadekoCommand, Aliases]
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
            await SendConfirmAsync(builder.ToString()).ConfigureAwait(false);
        }

        [NadekoCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task UserId([Leftover] IGuildUser target = null)
        {
            var usr = target ?? ctx.User;
            await ReplyConfirmLocalizedAsync(strs.userid("🆔", Format.Bold(usr.ToString()),
                Format.Code(usr.Id.ToString())));
        }

        [NadekoCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task RoleId([Leftover] IRole role)
        {
            await ReplyConfirmLocalizedAsync(strs.roleid("🆔", Format.Bold(role.ToString()),
                Format.Code(role.Id.ToString())));
        }

        [NadekoCommand, Aliases]
        public async Task ChannelId()
        {
            await ReplyConfirmLocalizedAsync(strs.channelid("🆔", Format.Code(ctx.Channel.Id.ToString())));
        }

        [NadekoCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ServerId()
        {
            await ReplyConfirmLocalizedAsync(strs.serverid("🆔", Format.Code(ctx.Guild.Id.ToString())));
        }

        [NadekoCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Roles(IGuildUser target, int page = 1)
        {
            var guild = ctx.Guild;

            const int rolesPerPage = 20;

            if (page < 1 || page > 100)
                return;

            if (target != null)
            {
                var roles = target.GetRoles().Except(new[] { guild.EveryoneRole }).OrderBy(r => -r.Position).Skip((page - 1) * rolesPerPage).Take(rolesPerPage).ToArray();
                if (!roles.Any())
                {
                    await ReplyErrorLocalizedAsync(strs.no_roles_on_page).ConfigureAwait(false);
                }
                else
                {

                    await SendConfirmAsync(GetText(strs.roles_page(page, Format.Bold(target.ToString()))),
                        "\n• " + string.Join("\n• ", (IEnumerable<IRole>)roles).SanitizeMentions(true)).ConfigureAwait(false);
                }
            }
            else
            {
                var roles = guild.Roles.Except(new[] { guild.EveryoneRole }).OrderBy(r => -r.Position).Skip((page - 1) * rolesPerPage).Take(rolesPerPage).ToArray();
                if (!roles.Any())
                {
                    await ReplyErrorLocalizedAsync(strs.no_roles_on_page).ConfigureAwait(false);
                }
                else
                {
                    await SendConfirmAsync(GetText(strs.roles_all_page(page)),
                        "\n• " + string.Join("\n• ", (IEnumerable<IRole>)roles).SanitizeMentions(true)).ConfigureAwait(false);
                }
            }
        }

        [NadekoCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task Roles(int page = 1) =>
            Roles(null, page);

        [NadekoCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ChannelTopic([Leftover]ITextChannel channel = null)
        {
            if (channel is null)
                channel = (ITextChannel)ctx.Channel;

            var topic = channel.Topic;
            if (string.IsNullOrWhiteSpace(topic))
                await ReplyErrorLocalizedAsync(strs.no_topic_set).ConfigureAwait(false);
            else
                await SendConfirmAsync(GetText(strs.channel_topic), topic).ConfigureAwait(false);
        }

        [NadekoCommand, Aliases]
        public async Task Stats()
        {
            var ownerIds = string.Join("\n", _creds.OwnerIds);
            if (string.IsNullOrWhiteSpace(ownerIds))
                ownerIds = "-";

            await ctx.Channel.EmbedAsync(
                    _eb.Create().WithOkColor()
                        .WithAuthor($"NadekoBot v{StatsService.BotVersion}",
                            "https://nadeko-pictures.nyc3.digitaloceanspaces.com/other/avatar.png",
                            "https://nadekobot.readthedocs.io/en/latest/")
                        .AddField(GetText(strs.author), _stats.Author, true)
                        .AddField(GetText(strs.botid), _client.CurrentUser.Id.ToString(), true)
                        .AddField(GetText(strs.shard), $"#{_client.ShardId} / {_creds.TotalShards}", true)
                        .AddField(GetText(strs.commands_ran), _stats.CommandsRan.ToString(), true)
                        .AddField(GetText(strs.messages), $"{_stats.MessageCounter} ({_stats.MessagesPerSecond:F2}/sec)",
                            true)
                        .AddField(GetText(strs.memory), FormattableString.Invariant($"{_stats.GetPrivateMemory():F2} MB"), true)
                        .AddField(GetText(strs.owner_ids), ownerIds, true)
                        .AddField(GetText(strs.uptime), _stats.GetUptimeString("\n"), true)
                        .AddField(GetText(strs.presence), 
                            GetText(strs.presence_txt(
                                _coord.GetGuildCount(),
                                _stats.TextChannels,
                                _stats.VoiceChannels)),
                        true))
                .ConfigureAwait(false);
        }

        [NadekoCommand, Aliases]
        public async Task Showemojis([Leftover] string _) // need to have the parameter so that the message.tags gets populated
        {
            var tags = ctx.Message.Tags.Where(t => t.Type == TagType.Emoji).Select(t => (Emote)t.Value);

            var result = string.Join("\n", tags.Select(m => GetText(strs.showemojis(m, m.Url))));

            if (string.IsNullOrWhiteSpace(result))
                await ReplyErrorLocalizedAsync(strs.showemojis_none).ConfigureAwait(false);
            else
                await ctx.Channel.SendMessageAsync(result.TrimTo(2000)).ConfigureAwait(false);
        }

        [NadekoCommand, Aliases]
        [OwnerOnly]
        public async Task ListServers(int page = 1)
        {
            page -= 1;

            if (page < 0)
                return;

            var guilds = await Task.Run(() => _client.Guilds.OrderBy(g => g.Name).Skip((page) * 15).Take(15)).ConfigureAwait(false);

            if (!guilds.Any())
            {
                await ReplyErrorLocalizedAsync(strs.listservers_none).ConfigureAwait(false);
                return;
            }

            var embed = _eb.Create()
                .WithOkColor();
            foreach (var guild in guilds)
                embed.AddField(guild.Name,
                    GetText(strs.listservers(guild.Id, guild.MemberCount, guild.OwnerId)),
                    false);

            await ctx.Channel.EmbedAsync(embed);
        }


        [NadekoCommand, Aliases]
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

        [NadekoCommand, Aliases]
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

                await SendConfirmAsync($"{Format.Bold(ctx.User.ToString())} 🏓 {(int)sw.Elapsed.TotalMilliseconds}ms").ConfigureAwait(false);
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
        //             await ReplyErrorLocalizedAsync(strs.your_invite($"https://discord.gg/{code}"));
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
        //     var embed = _eb.Create()
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
