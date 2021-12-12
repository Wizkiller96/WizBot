using Discord;
using Discord.Commands;
using Discord.WebSocket;
using WizBot.Extensions;
using WizBot.Services;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WizBot.Common.Attributes;

namespace WizBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class InfoCommands : WizBotSubmodule
        {
            private readonly DiscordSocketClient _client;
            private readonly IStatsService _stats;

            public InfoCommands(DiscordSocketClient client, IStatsService stats)
            {
                _client = client;
                _stats = stats;
            }

            [WizBotCommand, Aliases]
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
                var textchn = guild.TextChannels.Count;
                var voicechn = guild.VoiceChannels.Count;
                var channels = $@"{GetText(strs.text_channels(textchn))}
{GetText(strs.voice_channels(voicechn))}";
                var createdAt = new DateTime(2015, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(guild.Id >> 22);
                var features = string.Join(", ", guild.Features);
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
                        string.Join(" ", guild.Emotes
                            .Shuffle()
                            .Take(20)
                            .Select(e => $"{e.Name} {e.ToString()}"))
                            .TrimTo(1020));
                }
                await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [WizBotCommand, Aliases]
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
                    .AddField(GetText(strs.id), ch.Id.ToString(), true)
                    .AddField(GetText(strs.created_at), $"{createdAt:dd.MM.yyyy HH:mm}", true)
                    .AddField(GetText(strs.users), usercount.ToString(), true)
                    .WithOkColor();
                await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [WizBotCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task UserInfo(IGuildUser usr = null)
            {
                var user = usr ?? ctx.User as IGuildUser;

                if (user is null)
                    return;

                var embed = _eb.Create()
                    .AddField(GetText(strs.name), $"**{user.Username}**#{user.Discriminator}", true);
                if (!string.IsNullOrWhiteSpace(user.Nickname))
                {
                    embed.AddField(GetText(strs.nickname), user.Nickname, true);
                }
                // Custom WizBot Titles - Will find a better way to do this in the future for WizNet and WizBot Staff
                if (usr.Id == 99272781513920512)
                {
                    embed.AddField("WizBot Title", "🛠️ Developer", true);
                }
                if (usr.Id == 105635576866156544)
                {
                    embed.AddField("WizBot Title", "🛠️ Nadeko Creator", true);
                }
                else if (usr.Id == _client.CurrentUser.Id || usr.Id == 170849991357628416)
                {
                    embed.AddField("WizBot Title", "🤖 Bot", true);
                }
#if GLOBAL_WIZBOT
                // WizBot Staff ID(s)
                else if (usr.Id == 474972711798702090 || usr.Id == 216898612867629057 || usr.Id == 111566184448589824 || usr.Id == 169177230717616128 || usr.Id == 213817345334968330 || usr.Id == 313317616439525386)
                {
                    embed.AddField("WizBot Title", "<:WizBot:708689390934687787> WizBot Staff", true);
                }
                // WizNet Staff ID(s)
                else if (usr.Id == 110535921782247424)
                {
                    embed.AddField("WizBot Title", "<:WizNet:705993965073334382> WizNet Staff", true);
                }
#endif
                embed.AddField(GetText(strs.id), user.Id.ToString(), true)
                    .AddField(GetText(strs.joined_server), $"{user.JoinedAt?.ToString("dd.MM.yyyy HH:mm") ?? "?"}", true)
                    .AddField(GetText(strs.joined_discord), $"{user.CreatedAt:dd.MM.yyyy HH:mm}", true)
                    .AddField(GetText(strs.roles), $"**({user.RoleIds.Count - 1})** - {string.Join("\n", user.GetRoles().Take(10).Where(r => r.Id != r.Guild.EveryoneRole.Id).Select(r => r.Name)).SanitizeMentions(true)}", true)
                    .WithOkColor();

                var av = user.RealAvatarUrl();
                if (av != null && av.IsAbsoluteUri)
                    embed.WithThumbnailUrl(av.ToString());
                await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [WizBotCommand, Aliases]
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
                    str.AppendLine(GetText(strs.activity_line(
                        ++startCount,
                        Format.Bold(kvp.Key.ToString()),
                        kvp.Value / _stats.GetUptime().TotalSeconds, kvp.Value)));
                }

                await ctx.Channel.EmbedAsync(_eb.Create()
                    .WithTitle(GetText(strs.activity_page(page + 1)))
                    .WithOkColor()
                    .WithFooter(GetText(strs.activity_users_total(CmdHandler.UserMessagesSent.Count)))
                    .WithDescription(str.ToString())).ConfigureAwait(false);
            }
        }
    }
}
