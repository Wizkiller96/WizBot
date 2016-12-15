using Discord;
using Discord.Commands;
using WizBot.Attributes;
using System;
using System.Linq;
using System.Threading.Tasks;
using WizBot.Services;
using System.Text;
using WizBot.Extensions;
using System.Text.RegularExpressions;
using System.Reflection;
using Discord.WebSocket;
using WizBot.Services.Impl;
using Discord.API;
using Embed = Discord.API.Embed;
using EmbedAuthor = Discord.API.EmbedAuthor;
using EmbedField = Discord.API.EmbedField;

namespace WizBot.Modules.Utility
{

    [WizBotModule("Utility", ".")]
    public partial class Utility : DiscordModule
    {
        public Utility() : base()
        {

        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task WhosPlaying(IUserMessage umsg, [Remainder] string game = null)
        {
            var channel = (ITextChannel)umsg.Channel;
            game = game.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(game))
                return;
            var arr = (await (umsg.Channel as IGuildChannel).Guild.GetUsersAsync())
                    .Where(u => u.Game?.Name?.ToUpperInvariant() == game)
                    .Select(u => u.Username)
                    .ToList();

            int i = 0;
            if (!arr.Any())
                await channel.SendErrorAsync("Nobody is playing that game.").ConfigureAwait(false);
            else
                await channel.SendConfirmAsync("```css\n" + string.Join("\n", arr.GroupBy(item => (i++) / 2)
                                                                                 .Select(ig => string.Concat(ig.Select(el => $"• {el,-27}")))) + "\n```")
                                                                                 .ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task InRole(IUserMessage umsg, [Remainder] string roles)
        {
            if (string.IsNullOrWhiteSpace(roles))
                return;
            var channel = (ITextChannel)umsg.Channel;
            var arg = roles.Split(',').Select(r => r.Trim().ToUpperInvariant());
            string send = "ℹ️ **Here is a list of users in those roles:**";
            foreach (var roleStr in arg.Where(str => !string.IsNullOrWhiteSpace(str) && str != "@EVERYONE" && str != "EVERYONE"))
            {
                var role = channel.Guild.Roles.Where(r => r.Name.ToUpperInvariant() == roleStr).FirstOrDefault();
                if (role == null) continue;
                send += $"```css\n[{role.Name}]\n";
                send += string.Join(", ", channel.Guild.GetUsers().Where(u => u.Roles.Contains(role)).Select(u => u.ToString()));
                send += $"\n```";
            }
            var usr = umsg.Author as IGuildUser;
            while (send.Length > 2000)
            {
                if (!usr.GetPermissions(channel).ManageMessages)
                {
                    await channel.SendErrorAsync($"⚠️ {usr.Mention} **you are not allowed to use this command on roles with a lot of users in them to prevent abuse.**").ConfigureAwait(false);
                    return;
                }
                var curstr = send.Substring(0, 2000);
                await channel.SendConfirmAsync(curstr.Substring(0,
                        curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1)).ConfigureAwait(false);
                send = curstr.Substring(curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1) +
                       send.Substring(2000);
            }
            await channel.SendConfirmAsync(send).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task CheckMyPerms(IUserMessage msg)
        {

            StringBuilder builder = new StringBuilder("```http\n");
            var user = msg.Author as IGuildUser;
            var perms = user.GetPermissions((ITextChannel)msg.Channel);
            foreach (var p in perms.GetType().GetProperties().Where(p => !p.GetGetMethod().GetParameters().Any()))
            {
                builder.AppendLine($"{p.Name} : {p.GetValue(perms, null).ToString()}");
            }

            builder.Append("```");
            await msg.Channel.SendConfirmAsync(builder.ToString());
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task UserId(IUserMessage msg, IGuildUser target = null)
        {
            var usr = target ?? msg.Author;
            await msg.Channel.SendConfirmAsync($"🆔 of the user **{ usr.Username }** is `{ usr.Id }`").ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task ChannelId(IUserMessage msg)
        {
            await msg.Channel.SendConfirmAsync($"🆔 of this channel is `{msg.Channel.Id}`").ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ServerId(IUserMessage msg)
        {
            await msg.Channel.SendConfirmAsync($"🆔 of this server is `{((ITextChannel)msg.Channel).Guild.Id}`").ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Roles(IUserMessage msg, IGuildUser target, int page = 1)
        {
            var channel = (ITextChannel)msg.Channel;
            var guild = channel.Guild;

            const int RolesPerPage = 20;

            if (page < 1 || page > 100)
                return;
            if (target != null)
            {
                await channel.SendConfirmAsync($"⚔ **Page #{page} of roles for {target.Username}**", $"```css\n• " + string.Join("\n• ", target.Roles.Except(new[] { guild.EveryoneRole }).OrderBy(r => -r.Position).Skip((page - 1) * RolesPerPage).Take(RolesPerPage)).SanitizeMentions() + "\n```");
            }
            else
            {
                await channel.SendConfirmAsync($"⚔ **Page #{page} of all roles on this server:**", $"```css\n• " + string.Join("\n• ", guild.Roles.Except(new[] { guild.EveryoneRole }).OrderBy(r => -r.Position).Skip((page - 1) * RolesPerPage).Take(RolesPerPage)).SanitizeMentions() + "\n```");
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task Roles(IUserMessage msg, int page = 1) =>
            Roles(msg, null, page);

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ChannelTopic(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;

            var topic = channel.Topic;
            if (string.IsNullOrWhiteSpace(topic))
                await channel.SendErrorAsync("No topic set.");
            else
                await channel.SendConfirmAsync("Channel topic", topic);
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task Stats(IUserMessage umsg)
        {
            var channel = umsg.Channel;

            var stats = WizBot.Stats;

            await channel.EmbedAsync(
                new Embed()
                {
                    Author = new EmbedAuthor()
                    {
                        Name = $"WizBot v{StatsService.BotVersion}",
                        Url = "http://wizkiller96network.com/",
                        IconUrl = "https://cdn.discordapp.com/avatars/170849991357628416/412367ac7ffd3915a0b969f6f3e17aca.jpg"
                    },
                    Fields = new[] {
                        new EmbedField() {
                            Name = Format.Bold("Author"),
                            Value = stats.Author,
                            Inline = true
                        },
                        new EmbedField() {
                            Name = Format.Bold("Library"),
                            Value = stats.Library,
                            Inline = true
                        },
                        new EmbedField() {
                            Name = Format.Bold("Bot ID"),
                            Value = WizBot.Client.GetCurrentUser().Id.ToString(),
                            Inline = true
                        },
                        new EmbedField() {
                            Name = Format.Bold("Commands Ran"),
                            Value = stats.CommandsRan.ToString(),
                            Inline = true
                        },
                        new EmbedField() {
                            Name = Format.Bold("Messages"),
                            Value = $"{stats.MessageCounter} ({stats.MessagesPerSecond:F2}/sec)",
                            Inline = true
                        },
                        new EmbedField() {
                            Name = Format.Bold("Memory"),
                            Value = $"{stats.Heap} MB",
                            Inline = true
                        },
                        new EmbedField() {
                            Name = Format.Bold("Owner ID(s)"),
                            Value = stats.OwnerIds,
                            Inline = true
                        },
                        new EmbedField() {
                            Name = Format.Bold("Uptime"),
                            Value = stats.GetUptimeString("\n"),
                            Inline = true
                        },
                        new EmbedField() {
                            Name = Format.Bold("Presence"),
                            Value = $"{WizBot.Client.GetGuilds().Count} Servers\n{stats.TextChannels} Text Channels\n{stats.VoiceChannels} Voice Channels",
                            Inline = true
                        },

                    },
                    Color = 0x00bbd6
                });
        }

        private Regex emojiFinder { get; } = new Regex(@"<:(?<name>.+?):(?<id>\d*)>", RegexOptions.Compiled);
        [WizBotCommand, Usage, Description, Aliases]
        public async Task Showemojis(IUserMessage msg, [Remainder] string emojis)
        {
            var matches = emojiFinder.Matches(emojis);

            var result = string.Join("\n", matches.Cast<Match>()
                                                  .Select(m => $"**Name:** {m.Groups["name"]} **Link:** http://discordapp.com/api/emojis/{m.Groups["id"]}.png"));

            if (string.IsNullOrWhiteSpace(result))
                await msg.Channel.SendErrorAsync("No special emojis found.");
            else
                await msg.Channel.SendMessageAsync(result).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task ListServers(IUserMessage imsg, int page = 1)
        {
            var channel = (ITextChannel)imsg.Channel;

            page -= 1;

            if (page < 0)
                return;

            var guilds = WizBot.Client.GetGuilds().OrderBy(g => g.Name).Skip((page - 1) * 15).Take(15);

            if (!guilds.Any())
            {
                await channel.SendErrorAsync("No servers found on that page.").ConfigureAwait(false);
                return;
            }

            await channel.EmbedAsync(guilds.Aggregate(new EmbedBuilder().WithColor(WizBot.OkColor),
                                     (embed, g) => embed.AddField(efb => efb.WithName(g.Name)
                                                                           .WithValue($"```css\nID: {g.Id}\nMembers: {g.GetUsers().Count}\nOwnerID: {g.OwnerId} ```")
                                                                           .WithIsInline(false)))
                                           .Build())
                         .ConfigureAwait(false);
        }
    }
}
