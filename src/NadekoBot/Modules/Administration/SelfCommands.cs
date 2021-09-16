using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Common.Replacements;
using NadekoBot.Services.Database.Models;
using NadekoBot.Extensions;
using NadekoBot.Modules.Administration.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Services;
using Serilog;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class SelfCommands : NadekoSubmodule<SelfService>
        {
            private readonly DiscordSocketClient _client;
            private readonly IBotStrings _strings;
            private readonly ICoordinator _coord;

            public SelfCommands(DiscordSocketClient client, IBotStrings strings, ICoordinator coord)
            {
                _client = client;
                _strings = strings;
                _coord = coord;
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            [OwnerOnly]
            public async Task StartupCommandAdd([Leftover] string cmdText)
            {
                if (cmdText.StartsWith(Prefix + "die", StringComparison.InvariantCulture))
                    return;

                var guser = (IGuildUser)ctx.User;
                var cmd = new AutoCommand()
                {
                    CommandText = cmdText,
                    ChannelId = ctx.Channel.Id,
                    ChannelName = ctx.Channel.Name,
                    GuildId = ctx.Guild?.Id,
                    GuildName = ctx.Guild?.Name,
                    VoiceChannelId = guser.VoiceChannel?.Id,
                    VoiceChannelName = guser.VoiceChannel?.Name,
                    Interval = 0,
                };
                _service.AddNewAutoCommand(cmd);

                await ctx.Channel.EmbedAsync(_eb.Create().WithOkColor()
                    .WithTitle(GetText(strs.scadd))
                    .AddField(GetText(strs.server), cmd.GuildId is null ? $"-" : $"{cmd.GuildName}/{cmd.GuildId}", true)
                    .AddField(GetText(strs.channel), $"{cmd.ChannelName}/{cmd.ChannelId}", true)
                    .AddField(GetText(strs.command_text), cmdText, false));
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            [OwnerOnly]
            public async Task AutoCommandAdd(int interval, [Leftover] string cmdText)
            {
                if (cmdText.StartsWith(Prefix + "die", StringComparison.InvariantCulture))
                    return;

                if (interval < 5)
                    return;

                var guser = (IGuildUser)ctx.User;
                var cmd = new AutoCommand()
                {
                    CommandText = cmdText,
                    ChannelId = ctx.Channel.Id,
                    ChannelName = ctx.Channel.Name,
                    GuildId = ctx.Guild?.Id,
                    GuildName = ctx.Guild?.Name,
                    VoiceChannelId = guser.VoiceChannel?.Id,
                    VoiceChannelName = guser.VoiceChannel?.Name,
                    Interval = interval,
                };
                _service.AddNewAutoCommand(cmd);

                await ReplyConfirmLocalizedAsync(strs.autocmd_add(Format.Code(Format.Sanitize(cmdText)), cmd.Interval));
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task StartupCommandsList(int page = 1)
            {
                if (page-- < 1)
                    return;

                var scmds = _service.GetStartupCommands()
                    .Skip(page * 5)
                    .Take(5)
                    .ToList();
                
                if (scmds.Count == 0)
                {
                    await ReplyErrorLocalizedAsync(strs.startcmdlist_none).ConfigureAwait(false);
                }
                else
                {
                    var i = 0;
                    await SendConfirmAsync(
                        text: string.Join("\n", scmds
                        .Select(x => $@"```css
#{++i + page * 5}
[{GetText(strs.server)}]: {(x.GuildId.HasValue ? $"{x.GuildName} #{x.GuildId}" : "-")}
[{GetText(strs.channel)}]: {x.ChannelName} #{x.ChannelId}
[{GetText(strs.command_text)}]: {x.CommandText}```")),
                        title: string.Empty,
                        footer: GetText(strs.page(page + 1)))
                    .ConfigureAwait(false);
                }
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task AutoCommandsList(int page = 1)
            {
                if (page-- < 1)
                    return;

                var scmds = _service.GetAutoCommands()
                    .Skip(page * 5)
                    .Take(5)
                    .ToList();
                if (!scmds.Any())
                {
                    await ReplyErrorLocalizedAsync(strs.autocmdlist_none).ConfigureAwait(false);
                }
                else
                {
                    var i = 0;
                    await SendConfirmAsync(
                        text: string.Join("\n", scmds
                        .Select(x => $@"```css
#{++i + page * 5}
[{GetText(strs.server)}]: {(x.GuildId.HasValue ? $"{x.GuildName} #{x.GuildId}" : "-")}
[{GetText(strs.channel)}]: {x.ChannelName} #{x.ChannelId}
{GetIntervalText(x.Interval)}
[{GetText(strs.command_text)}]: {x.CommandText}```")),
                        title: string.Empty,
                        footer: GetText(strs.page(page + 1)))
                    .ConfigureAwait(false);
                }
            }

            private string GetIntervalText(int interval)
            {
                return $"[{GetText(strs.interval)}]: {interval}";
            }

            [NadekoCommand, Aliases]
            [OwnerOnly]
            public async Task Wait(int miliseconds)
            {
                if (miliseconds <= 0)
                    return;
                ctx.Message.DeleteAfter(0);
                try
                {
                    var msg = await SendConfirmAsync($"⏲ {miliseconds}ms")
                        .ConfigureAwait(false);
                    msg.DeleteAfter(miliseconds / 1000);
                }
                catch { }

                await Task.Delay(miliseconds).ConfigureAwait(false);
            }
            
            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            [OwnerOnly]
            public async Task AutoCommandRemove([Leftover] int index)
            {
                if (!_service.RemoveAutoCommand(--index, out _))
                {
                    await ReplyErrorLocalizedAsync(strs.acrm_fail).ConfigureAwait(false);
                    return;
                }
                
                await ctx.OkAsync();
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task StartupCommandRemove([Leftover] int index)
            {
                if (!_service.RemoveStartupCommand(--index, out _))
                    await ReplyErrorLocalizedAsync(strs.scrm_fail).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalizedAsync(strs.scrm).ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            [OwnerOnly]
            public async Task StartupCommandsClear()
            {
                _service.ClearStartupCommands();

                await ReplyConfirmLocalizedAsync(strs.startcmds_cleared).ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
            [OwnerOnly]
            public async Task ForwardMessages()
            {
                var enabled = _service.ForwardMessages();

                if (enabled)
                    await ReplyConfirmLocalizedAsync(strs.fwdm_start).ConfigureAwait(false);
                else
                    await ReplyPendingLocalizedAsync(strs.fwdm_stop).ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
            [OwnerOnly]
            public async Task ForwardToAll()
            {
                var enabled = _service.ForwardToAll();

                if (enabled)
                    await ReplyConfirmLocalizedAsync(strs.fwall_start).ConfigureAwait(false);
                else
                    await ReplyPendingLocalizedAsync(strs.fwall_stop).ConfigureAwait(false);

            }

            [NadekoCommand, Aliases]
            public async Task ShardStats(int page = 1)
            {
                if (--page < 0)
                    return;

                var statuses = _coord.GetAllShardStatuses();

                var status = string.Join(" : ", statuses
                    .Select(x => (ConnectionStateToEmoji(x), x))
                    .GroupBy(x => x.Item1)
                    .Select(x => $"`{x.Count()} {x.Key}`")
                    .ToArray());

                var allShardStrings = statuses
                    .Select(st =>
                    {
                        var stateStr = ConnectionStateToEmoji(st);
                        var timeDiff = DateTime.UtcNow - st.LastUpdate;
                        var maxGuildCountLength = statuses.Max(x => x.GuildCount).ToString().Length;
                        return $"`{stateStr} " +
                               $"| #{st.ShardId.ToString().PadBoth(3)} " +
                               $"| {timeDiff:mm\\:ss} " +
                               $"| {st.GuildCount.ToString().PadBoth(maxGuildCountLength)} `";
                    })
                    .ToArray();
                await ctx.SendPaginatedConfirmAsync(page, (curPage) =>
                {
                    var str = string.Join("\n", allShardStrings.Skip(25 * curPage).Take(25));

                    if (string.IsNullOrWhiteSpace(str))
                        str = GetText(strs.no_shards_on_page);

                    return _eb.Create()
                        .WithOkColor()
                        .WithDescription($"{status}\n\n{str}");
                }, allShardStrings.Length, 25).ConfigureAwait(false);
            }

            private static string ConnectionStateToEmoji(ShardStatus status)
            {
                var timeDiff = DateTime.UtcNow - status.LastUpdate;
                return status.ConnectionState switch
                {
                    ConnectionState.Connected => "✅",
                    ConnectionState.Disconnected => "🔻",
                    _ when timeDiff > TimeSpan.FromSeconds(30) => " ❗ ",
                    _ => " ⏳"
                };
            }

            [NadekoCommand, Aliases]
            [OwnerOnly]
            public async Task RestartShard(int shardId)
            {
                var success = _coord.RestartShard(shardId);
                if (success)
                {
                    await ReplyConfirmLocalizedAsync(strs.shard_reconnecting(Format.Bold("#" + shardId))).ConfigureAwait(false);
                }
                else
                {
                    await ReplyErrorLocalizedAsync(strs.no_shard_id).ConfigureAwait(false);
                }
            }

            [NadekoCommand, Aliases]
            [OwnerOnly]
            public Task Leave([Leftover] string guildStr)
            {
                return _service.LeaveGuild(guildStr);
            }


            [NadekoCommand, Aliases]
            [OwnerOnly]
            public async Task Die()
            {
                try
                {
                    await ReplyConfirmLocalizedAsync(strs.shutting_down).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
                await Task.Delay(2000).ConfigureAwait(false);
                _coord.Die();
            }

            [NadekoCommand, Aliases]
            [OwnerOnly]
            public async Task Restart()
            {
                bool success = _coord.RestartBot();
                if (!success)
                {
                    await ReplyErrorLocalizedAsync(strs.restart_fail).ConfigureAwait(false);
                    return;
                }

                try { await ReplyConfirmLocalizedAsync(strs.restarting).ConfigureAwait(false); } catch { }
            }

            [NadekoCommand, Aliases]
            [OwnerOnly]
            public async Task SetName([Leftover] string newName)
            {
                if (string.IsNullOrWhiteSpace(newName))
                    return;

                try
                {
                    await _client.CurrentUser.ModifyAsync(u => u.Username = newName).ConfigureAwait(false);
                }
                catch (RateLimitedException)
                {
                    Log.Warning("You've been ratelimited. Wait 2 hours to change your name");
                }

                await ReplyConfirmLocalizedAsync(strs.bot_name(Format.Bold(newName))).ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
            [UserPerm(GuildPerm.ManageNicknames)]
            [BotPerm(GuildPerm.ChangeNickname)]
            [Priority(0)]
            public async Task SetNick([Leftover] string newNick = null)
            {
                if (string.IsNullOrWhiteSpace(newNick))
                    return;
                var curUser = await ctx.Guild.GetCurrentUserAsync().ConfigureAwait(false);
                await curUser.ModifyAsync(u => u.Nickname = newNick).ConfigureAwait(false);

                await ReplyConfirmLocalizedAsync(strs.bot_nick(Format.Bold(newNick) ?? "-"));
            }

            [NadekoCommand, Aliases]
            [BotPerm(GuildPerm.ManageNicknames)]
            [UserPerm(GuildPerm.ManageNicknames)]
            [Priority(1)]
            public async Task SetNick(IGuildUser gu, [Leftover] string newNick = null)
            {
                var sg = (SocketGuild) ctx.Guild;
                if (sg.OwnerId == gu.Id ||
                    gu.GetRoles().Max(r => r.Position) >= sg.CurrentUser.GetRoles().Max(r => r.Position))
                {
                    await ReplyErrorLocalizedAsync(strs.insuf_perms_i);
                    return;
                }
                
                await gu.ModifyAsync(u => u.Nickname = newNick).ConfigureAwait(false);

                await ReplyConfirmLocalizedAsync(strs.user_nick(Format.Bold(gu.ToString()), Format.Bold(newNick) ?? "-"));
            }

            [NadekoCommand, Aliases]
            [OwnerOnly]
            public async Task SetStatus([Leftover] SettableUserStatus status)
            {
                await _client.SetStatusAsync(SettableUserStatusToUserStatus(status)).ConfigureAwait(false);

                await ReplyConfirmLocalizedAsync(strs.bot_status(Format.Bold(status.ToString()))).ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
            [OwnerOnly]
            public async Task SetAvatar([Leftover] string img = null)
            {
                var success = await _service.SetAvatar(img);

                if (success)
                {
                    await ReplyConfirmLocalizedAsync(strs.set_avatar).ConfigureAwait(false);
                }
            }

            [NadekoCommand, Aliases]
            [OwnerOnly]
            public async Task SetGame(ActivityType type, [Leftover] string game = null)
            {
                var rep = new ReplacementBuilder()
                    .WithDefault(Context)
                    .Build();

                await _service.SetGameAsync(game is null ? game : rep.Replace(game), type).ConfigureAwait(false);

                await ReplyConfirmLocalizedAsync(strs.set_game).ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
            [OwnerOnly]
            public async Task SetStream(string url, [Leftover] string name = null)
            {
                name = name ?? "";

                await _service.SetStreamAsync(name, url).ConfigureAwait(false);

                await ReplyConfirmLocalizedAsync(strs.set_stream).ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
            [OwnerOnly]
            public async Task Send(string where, [Leftover] SmartText text = null)
            {
                var ids = where.Split('|');
                if (ids.Length != 2)
                    return;
                
                var sid = ulong.Parse(ids[0]);
                var server = _client.Guilds.FirstOrDefault(s => s.Id == sid);

                if (server is null)
                    return;

                var rep = new ReplacementBuilder()
                    .WithDefault(Context)
                    .Build();

                if (ids[1].ToUpperInvariant().StartsWith("C:", StringComparison.InvariantCulture))
                {
                    var cid = ulong.Parse(ids[1].Substring(2));
                    var ch = server.TextChannels.FirstOrDefault(c => c.Id == cid);
                    if (ch is null)
                        return;

                    text = rep.Replace(text);
                    await ch.SendAsync(text, sanitizeAll: false);
                }
                else if (ids[1].ToUpperInvariant().StartsWith("U:", StringComparison.InvariantCulture))
                {
                    var uid = ulong.Parse(ids[1].Substring(2));
                    var user = server.Users.FirstOrDefault(u => u.Id == uid);
                    if (user is null)
                        return;

                    var ch = await user.GetOrCreateDMChannelAsync();
                    text = rep.Replace(text);
                    await ch.SendAsync(text);
                }
                else
                {
                    await ReplyErrorLocalizedAsync(strs.invalid_format).ConfigureAwait(false);
                    return;
                }
                
                await ReplyConfirmLocalizedAsync(strs.message_sent).ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
            [OwnerOnly]
            public async Task ImagesReload()
            {
                await _service.ReloadImagesAsync();
                await ReplyConfirmLocalizedAsync(strs.images_loading);
            }
            
            [NadekoCommand, Aliases]
            [OwnerOnly]
            public async Task StringsReload()
            {
                _strings.Reload();
                await ReplyConfirmLocalizedAsync(strs.bot_strings_reloaded).ConfigureAwait(false);
            }

            private static UserStatus SettableUserStatusToUserStatus(SettableUserStatus sus)
            {
                switch (sus)
                {
                    case SettableUserStatus.Online:
                        return UserStatus.Online;
                    case SettableUserStatus.Invisible:
                        return UserStatus.Invisible;
                    case SettableUserStatus.Idle:
                        return UserStatus.AFK;
                    case SettableUserStatus.Dnd:
                        return UserStatus.DoNotDisturb;
                }

                return UserStatus.Online;
            }

            public enum SettableUserStatus
            {
                Online,
                Invisible,
                Idle,
                Dnd
            }
        }
    }
}