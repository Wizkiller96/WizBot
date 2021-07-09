using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Services;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class InfoCommands : NadekoSubmodule
        {
            private readonly DiscordSocketClient _client;
            private readonly IStatsService _stats;

            public InfoCommands(DiscordSocketClient client, IStatsService stats)
            {
                _client = client;
                _stats = stats;
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task ServerInfo(string guildName = null)
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
                var textchn = guild.TextChannels.Count();
                var voicechn = guild.VoiceChannels.Count();

                var createdAt = new DateTime(2015, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(guild.Id >> 22);
                var features = string.Join("\n", guild.Features);
                if (string.IsNullOrWhiteSpace(features))
                    features = "-";
                var embed = _eb.Create()
                    .WithAuthor(GetText("server_info"))
                    .WithTitle(guild.Name)
                    .AddField(GetText("id"), guild.Id.ToString(), true)
                    .AddField(GetText("owner"), ownername.ToString(), true)
                    .AddField(GetText("members"), guild.MemberCount.ToString(), true)
                    .AddField(GetText("text_channels"), textchn.ToString(), true)
                    .AddField(GetText("voice_channels"), voicechn.ToString(), true)
                    .AddField(GetText("created_at"), $"{createdAt:dd.MM.yyyy HH:mm}", true)
                    .AddField(GetText("region"), guild.VoiceRegionId.ToString(), true)
                    .AddField(GetText("roles"), (guild.Roles.Count - 1).ToString(), true)
                    .AddField(GetText("features"), features, true)
                    .WithOkColor();
                if (Uri.IsWellFormedUriString(guild.IconUrl, UriKind.Absolute))
                    embed.WithThumbnailUrl(guild.IconUrl);
                if (guild.Emotes.Any())
                {
                    embed.AddField(GetText("custom_emojis") + $"({guild.Emotes.Count})",
                        string.Join(" ", guild.Emotes
                            .Shuffle()
                            .Take(20)
                            .Select(e => $"{e.Name} {e.ToString()}"))
                            .TrimTo(1020));
                }
                await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task ChannelInfo(ITextChannel channel = null)
            {
                var ch = channel ?? (ITextChannel)ctx.Channel;
                if (ch is null)
                    return;
                var createdAt = new DateTime(2015, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(ch.Id >> 22);
                var usercount = (await ch.GetUsersAsync().FlattenAsync().ConfigureAwait(false)).Count();
                var embed = _eb.Create()
                    .WithTitle(ch.Name)
                    .WithDescription(ch.Topic?.SanitizeMentions(true))
                    .AddField(GetText("id"), ch.Id.ToString(), true)
                    .AddField(GetText("created_at"), $"{createdAt:dd.MM.yyyy HH:mm}", true)
                    .AddField(GetText("users"), usercount.ToString(), true)
                    .WithOkColor();
                await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task UserInfo(IGuildUser usr = null)
            {
                var user = usr ?? ctx.User as IGuildUser;

                if (user is null)
                    return;

                var embed = _eb.Create()
                    .AddField(GetText("name"), $"**{user.Username}**#{user.Discriminator}", true);
                if (!string.IsNullOrWhiteSpace(user.Nickname))
                {
                    embed.AddField(GetText("nickname"), user.Nickname, true);
                }
                embed.AddField(GetText("id"), user.Id.ToString(), true)
                    .AddField(GetText("joined_server"), $"{user.JoinedAt?.ToString("dd.MM.yyyy HH:mm") ?? "?"}", true)
                    .AddField(GetText("joined_discord"), $"{user.CreatedAt:dd.MM.yyyy HH:mm}", true)
                    .AddField(GetText("roles"), $"**({user.RoleIds.Count - 1})** - {string.Join("\n", user.GetRoles().Take(10).Where(r => r.Id != r.Guild.EveryoneRole.Id).Select(r => r.Name)).SanitizeMentions(true)}", true)
                    .WithOkColor();

                var av = user.RealAvatarUrl();
                if (av != null && av.IsAbsoluteUri)
                    embed.WithThumbnailUrl(av.ToString());
                await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task Activity(int page = 1)
            {
                const int activityPerPage = 10;
                page -= 1;

                if (page < 0)
                    return;

                int startCount = page * activityPerPage;

                StringBuilder str = new StringBuilder();
                foreach (var kvp in CmdHandler.UserMessagesSent.OrderByDescending(kvp => kvp.Value).Skip(page * activityPerPage).Take(activityPerPage))
                {
                    str.AppendLine(GetText("activity_line",
                        ++startCount,
                        Format.Bold(kvp.Key.ToString()),
                        kvp.Value / _stats.GetUptime().TotalSeconds, kvp.Value));
                }

                await ctx.Channel.EmbedAsync(_eb.Create()
                    .WithTitle(GetText("activity_page", page + 1))
                    .WithOkColor()
                    .WithFooter(GetText("activity_users_total", CmdHandler.UserMessagesSent.Count))
                    .WithDescription(str.ToString())).ConfigureAwait(false);
            }
        }
    }
}
