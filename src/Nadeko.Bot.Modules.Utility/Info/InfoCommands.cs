#nullable disable
using NadekoBot.Modules.Utility.Patronage;
using System.Text;
using Nadeko.Common;

namespace NadekoBot.Modules.Utility;

public partial class Utility
{
    [Group]
    public partial class InfoCommands : NadekoModule
    {
        private readonly DiscordSocketClient _client;
        private readonly IStatsService _stats;
        private readonly IPatronageService _ps;

        public InfoCommands(DiscordSocketClient client, IStatsService stats, IPatronageService ps)
        {
            _client = client;
            _stats = stats;
            _ps = ps;
        }

        [Cmd]
        [OwnerOnly]
        public Task ServerInfo([Leftover] string guildName)
            => InternalServerInfo(guildName);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public Task ServerInfo()
            => InternalServerInfo();

        private async Task InternalServerInfo(string guildName = null)
        {
            var channel = (ITextChannel)ctx.Channel;
            guildName = guildName?.ToUpperInvariant();
            SocketGuild guild;

            if (string.IsNullOrWhiteSpace(guildName))
                guild = (SocketGuild)channel.Guild;
            else
                guild = _client.Guilds.FirstOrDefault(g => g.Name.ToUpperInvariant() == guildName.ToUpperInvariant());

            if (guild is null)
                return;

            var ownername = guild.GetUser(guild.OwnerId);
            var textchn = guild.TextChannels.Count;
            var voicechn = guild.VoiceChannels.Count;
            var channels = $@"{GetText(strs.text_channels(textchn))}
{GetText(strs.voice_channels(voicechn))}";
            var createdAt = new DateTime(2015, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(guild.Id >> 22);
            var features = guild.Features.Value.ToString();
            if (string.IsNullOrWhiteSpace(features))
                features = "-";

            var embed = _eb.Create()
                           .WithAuthor(GetText(strs.server_info))
                           .WithTitle(guild.Name)
                           .AddField(GetText(strs.id), guild.Id.ToString(), true)
                           .AddField(GetText(strs.owner), ownername.ToString(), true)
                           .AddField(GetText(strs.members), guild.MemberCount.ToString(), true)
                           .AddField(GetText(strs.channels), channels, true)
                           .AddField(GetText(strs.created_at), $"{createdAt:dd.MM.yyyy HH:mm}", true)
                           .AddField(GetText(strs.roles), (guild.Roles.Count - 1).ToString(), true)
                           .AddField(GetText(strs.features), features)
                           .WithOkColor();

            if (Uri.IsWellFormedUriString(guild.IconUrl, UriKind.Absolute))
                embed.WithThumbnailUrl(guild.IconUrl);

            if (guild.Emotes.Any())
            {
                embed.AddField(GetText(strs.custom_emojis) + $"({guild.Emotes.Count})",
                    string.Join(" ", guild.Emotes.Shuffle().Take(20).Select(e => $"{e.Name} {e}")).TrimTo(1020));
            }

            await ctx.Channel.EmbedAsync(embed);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task ChannelInfo(ITextChannel channel = null)
        {
            var ch = channel ?? (ITextChannel)ctx.Channel;
            if (ch is null)
                return;
            var createdAt = new DateTime(2015, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(ch.Id >> 22);
            var usercount = (await ch.GetUsersAsync().FlattenAsync()).Count();
            var embed = _eb.Create()
                           .WithTitle(ch.Name)
                           .WithDescription(ch.Topic?.SanitizeMentions(true))
                           .AddField(GetText(strs.id), ch.Id.ToString(), true)
                           .AddField(GetText(strs.created_at), $"{createdAt:dd.MM.yyyy HH:mm}", true)
                           .AddField(GetText(strs.users), usercount.ToString(), true)
                           .WithOkColor();
            await ctx.Channel.EmbedAsync(embed);
        }
        
        [Cmd]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task RoleInfo([Leftover] SocketRole role)
        {
            if (role.IsEveryone)
                return;
            
            var createdAt = new DateTime(2015, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                .AddMilliseconds(role.Id >> 22);
            var usercount = role.Members.LongCount();
            var embed = _eb.Create()
                .WithTitle(role.Name.TrimTo(128))
                .WithDescription(role.Permissions.ToList().Join(" | "))
                .AddField(GetText(strs.id), role.Id.ToString(), true)
                .AddField(GetText(strs.created_at), $"{createdAt:dd.MM.yyyy HH:mm}", true)
                .AddField(GetText(strs.users), usercount.ToString(), true)
                .AddField(GetText(strs.color), $"#{role.Color.R:X2}{role.Color.G:X2}{role.Color.B:X2}", true)
                .AddField(GetText(strs.mentionable), role.IsMentionable.ToString(), true)
                .AddField(GetText(strs.hoisted), role.IsHoisted.ToString(), true)
                .WithOkColor();

            if (!string.IsNullOrWhiteSpace(role.GetIconUrl()))
                embed = embed.WithThumbnailUrl(role.GetIconUrl());
            
            await ctx.Channel.EmbedAsync(embed);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task UserInfo(IGuildUser usr = null)
        {
            var user = usr ?? ctx.User as IGuildUser;

            if (user is null)
                return;

            var embed = _eb.Create().AddField(GetText(strs.name), $"**{user.Username}**#{user.Discriminator}", true);
            if (!string.IsNullOrWhiteSpace(user.Nickname))
                embed.AddField(GetText(strs.nickname), user.Nickname, true);

            var joinedAt = GetJoinedAt(user);
            
            embed.AddField(GetText(strs.id), user.Id.ToString(), true)
                 .AddField(GetText(strs.joined_server), $"{joinedAt?.ToString("dd.MM.yyyy HH:mm") ?? "?"}", true)
                 .AddField(GetText(strs.joined_discord), $"{user.CreatedAt:dd.MM.yyyy HH:mm}", true)
                 .AddField(GetText(strs.roles),
                     $"**({user.RoleIds.Count - 1})** - {string.Join("\n", user.GetRoles().Take(10).Where(r => r.Id != r.Guild.EveryoneRole.Id).Select(r => r.Name)).SanitizeMentions(true)}",
                     true)
                 .WithOkColor();

            var patron = await _ps.GetPatronAsync(user.Id);
            
            if (patron.Tier != PatronTier.None)
            {
                embed.WithFooter(patron.Tier switch
                {
                    PatronTier.V => "❤️❤️",
                    PatronTier.X => "❤️❤️❤️",
                    PatronTier.XX => "❤️❤️❤️❤️",
                    PatronTier.L => "❤️❤️❤️❤️❤️",
                    _ => "❤️",
                });
            }
            
            var av = user.RealAvatarUrl();
            if (av.IsAbsoluteUri)
                embed.WithThumbnailUrl(av.ToString());
            
            await ctx.Channel.EmbedAsync(embed);
        }

        private DateTimeOffset? GetJoinedAt(IGuildUser user)
        {
            var joinedAt = user.JoinedAt;
            if (user.GuildId != 117523346618318850)
                return joinedAt;

            if (user.Id == 351244576092192778)
                return new DateTimeOffset(2019, 12, 25, 9, 33, 0, TimeSpan.Zero);

            return joinedAt;
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task Activity(int page = 1)
        {
            const int activityPerPage = 10;
            page -= 1;

            if (page < 0)
                return;

            var startCount = page * activityPerPage;

            var str = new StringBuilder();
            foreach (var kvp in _cmdHandler.UserMessagesSent.OrderByDescending(kvp => kvp.Value)
                                           .Skip(page * activityPerPage)
                                           .Take(activityPerPage))
            {
                str.AppendLine(GetText(strs.activity_line(++startCount,
                    Format.Bold(kvp.Key.ToString()),
                    kvp.Value / _stats.GetUptime().TotalSeconds,
                    kvp.Value)));
            }

            await ctx.Channel.EmbedAsync(_eb.Create()
                                            .WithTitle(GetText(strs.activity_page(page + 1)))
                                            .WithOkColor()
                                            .WithFooter(GetText(
                                                strs.activity_users_total(_cmdHandler.UserMessagesSent.Count)))
                                            .WithDescription(str.ToString()));
        }
    }
}