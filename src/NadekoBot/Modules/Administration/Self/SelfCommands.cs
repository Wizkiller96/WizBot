#nullable disable
using NadekoBot.Modules.Administration.Services;
using NadekoBot.Db.Models;
using Nadeko.Common.Medusa;

namespace NadekoBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class SelfCommands : NadekoModule<SelfService>
    {
        public enum SettableUserStatus
        {
            Online,
            Invisible,
            Idle,
            Dnd
        }

        private readonly DiscordSocketClient _client;
        private readonly IBotStrings _strings;
        private readonly IMedusaLoaderService _medusaLoader;
        private readonly ICoordinator _coord;
        private readonly DbService _db;

        public SelfCommands(
            DiscordSocketClient client,
            DbService db,
            IBotStrings strings,
            ICoordinator coord,
            IMedusaLoaderService medusaLoader)
        {
            _client = client;
            _db = db;
            _strings = strings;
            _coord = coord;
            _medusaLoader = medusaLoader;
        }


        [Cmd]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public Task CacheUsers()
            => CacheUsers(ctx.Guild);

        [Cmd]
        [OwnerOnly]
        public async Task CacheUsers(IGuild guild)
        {
            var downloadUsersTask = guild.DownloadUsersAsync();
            var message = await Response().Pending(strs.cache_users_pending).SendAsync();
            using var dbContext = _db.GetDbContext();

            await downloadUsersTask;

            var users = (await guild.GetUsersAsync(CacheMode.CacheOnly))
                        .Cast<IUser>()
                        .ToList();

            var (added, updated) = await _service.RefreshUsersAsync(users);

            await message.ModifyAsync(x =>
                x.Embed = new EmbedBuilder()
                             .WithDescription(GetText(strs.cache_users_done(added, updated)))
                             .WithOkColor()
                             .Build()
            );
        }

        [Cmd]
        [OwnerOnly]
        public async Task DoAs(IUser user, [Leftover] string message)
        {
            if (ctx.User is not IGuildUser { GuildPermissions.Administrator: true })
                return;

            if (ctx.Guild is SocketGuild sg
                && ctx.Channel is ISocketMessageChannel ch
                && ctx.Message is SocketUserMessage msg)
            {
                var fakeMessage = new DoAsUserMessage(msg, user, message);

                await _cmdHandler.TryRunCommand(sg, ch, fakeMessage);
            }
            else
            {
                await Response().Error(strs.error_occured).SendAsync();
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [OwnerOnly]
        public async Task StartupCommandAdd([Leftover] string cmdText)
        {
            if (cmdText.StartsWith(prefix + "die", StringComparison.InvariantCulture))
                return;

            var guser = (IGuildUser)ctx.User;
            var cmd = new AutoCommand
            {
                CommandText = cmdText,
                ChannelId = ctx.Channel.Id,
                ChannelName = ctx.Channel.Name,
                GuildId = ctx.Guild?.Id,
                GuildName = ctx.Guild?.Name,
                VoiceChannelId = guser.VoiceChannel?.Id,
                VoiceChannelName = guser.VoiceChannel?.Name,
                Interval = 0
            };
            _service.AddNewAutoCommand(cmd);

            await Response().Embed(new EmbedBuilder()
                                .WithOkColor()
                                .WithTitle(GetText(strs.scadd))
                                .AddField(GetText(strs.server),
                                    cmd.GuildId is null ? "-" : $"{cmd.GuildName}/{cmd.GuildId}",
                                    true)
                                .AddField(GetText(strs.channel), $"{cmd.ChannelName}/{cmd.ChannelId}", true)
                                .AddField(GetText(strs.command_text), cmdText)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [OwnerOnly]
        public async Task AutoCommandAdd(int interval, [Leftover] string cmdText)
        {
            if (cmdText.StartsWith(prefix + "die", StringComparison.InvariantCulture))
                return;

            if (interval < 5)
                return;

            var guser = (IGuildUser)ctx.User;
            var cmd = new AutoCommand
            {
                CommandText = cmdText,
                ChannelId = ctx.Channel.Id,
                ChannelName = ctx.Channel.Name,
                GuildId = ctx.Guild?.Id,
                GuildName = ctx.Guild?.Name,
                VoiceChannelId = guser.VoiceChannel?.Id,
                VoiceChannelName = guser.VoiceChannel?.Name,
                Interval = interval
            };
            _service.AddNewAutoCommand(cmd);

            await Response().Confirm(strs.autocmd_add(Format.Code(Format.Sanitize(cmdText)), cmd.Interval)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task StartupCommandsList(int page = 1)
        {
            if (page-- < 1)
                return;

            var scmds = _service.GetStartupCommands().Skip(page * 5).Take(5).ToList();

            if (scmds.Count == 0)
                await Response().Error(strs.startcmdlist_none).SendAsync();
            else
            {
                var i = 0;
                await Response()
                      .Confirm(text: string.Join("\n",
                              scmds.Select(x => $@"```css
#{++i + (page * 5)}
[{GetText(strs.server)}]: {(x.GuildId.HasValue ? $"{x.GuildName} #{x.GuildId}" : "-")}
[{GetText(strs.channel)}]: {x.ChannelName} #{x.ChannelId}
[{GetText(strs.command_text)}]: {x.CommandText}```")),
                          title: string.Empty,
                          footer: GetText(strs.page(page + 1)))
                      .SendAsync();
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task AutoCommandsList(int page = 1)
        {
            if (page-- < 1)
                return;

            var scmds = _service.GetAutoCommands().Skip(page * 5).Take(5).ToList();
            if (!scmds.Any())
                await Response().Error(strs.autocmdlist_none).SendAsync();
            else
            {
                var i = 0;
                await Response()
                      .Confirm(text: string.Join("\n",
                              scmds.Select(x => $@"```css
#{++i + (page * 5)}
[{GetText(strs.server)}]: {(x.GuildId.HasValue ? $"{x.GuildName} #{x.GuildId}" : "-")}
[{GetText(strs.channel)}]: {x.ChannelName} #{x.ChannelId}
{GetIntervalText(x.Interval)}
[{GetText(strs.command_text)}]: {x.CommandText}```")),
                          title: string.Empty,
                          footer: GetText(strs.page(page + 1)))
                      .SendAsync();
            }
        }

        private string GetIntervalText(int interval)
            => $"[{GetText(strs.interval)}]: {interval}";

        [Cmd]
        [OwnerOnly]
        public async Task Wait(int miliseconds)
        {
            if (miliseconds <= 0)
                return;
            ctx.Message.DeleteAfter(0);
            try
            {
                var msg = await Response().Confirm($"⏲ {miliseconds}ms").SendAsync();
                msg.DeleteAfter(miliseconds / 1000);
            }
            catch { }

            await Task.Delay(miliseconds);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [OwnerOnly]
        public async Task AutoCommandRemove([Leftover] int index)
        {
            if (!_service.RemoveAutoCommand(--index, out _))
            {
                await Response().Error(strs.acrm_fail).SendAsync();
                return;
            }

            await ctx.OkAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task StartupCommandRemove([Leftover] int index)
        {
            if (!_service.RemoveStartupCommand(--index, out _))
                await Response().Error(strs.scrm_fail).SendAsync();
            else
                await Response().Confirm(strs.scrm).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [OwnerOnly]
        public async Task StartupCommandsClear()
        {
            _service.ClearStartupCommands();

            await Response().Confirm(strs.startcmds_cleared).SendAsync();
        }

        [Cmd]
        [OwnerOnly]
        public async Task ForwardMessages()
        {
            var enabled = _service.ForwardMessages();

            if (enabled)
                await Response().Confirm(strs.fwdm_start).SendAsync();
            else
                await Response().Pending(strs.fwdm_stop).SendAsync();
        }

        [Cmd]
        [OwnerOnly]
        public async Task ForwardToAll()
        {
            var enabled = _service.ForwardToAll();

            if (enabled)
                await Response().Confirm(strs.fwall_start).SendAsync();
            else
                await Response().Pending(strs.fwall_stop).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task ForwardToChannel()
        {
            var enabled = _service.ForwardToChannel(ctx.Channel.Id);

            if (enabled)
                await Response().Confirm(strs.fwch_start).SendAsync();
            else
                await Response().Pending(strs.fwch_stop).SendAsync();
        }

        [Cmd]
        public async Task ShardStats(int page = 1)
        {
            if (--page < 0)
                return;

            var statuses = _coord.GetAllShardStatuses();

            var status = string.Join(" : ",
                statuses.Select(x => (ConnectionStateToEmoji(x), x))
                        .GroupBy(x => x.Item1)
                        .Select(x => $"`{x.Count()} {x.Key}`")
                        .ToArray());

            var allShardStrings = statuses.Select(st =>
                                          {
                                              var timeDiff = DateTime.UtcNow - st.LastUpdate;
                                              var stateStr = ConnectionStateToEmoji(st);
                                              var maxGuildCountLength =
                                                  statuses.Max(x => x.GuildCount).ToString().Length;
                                              return $"`{stateStr} "
                                                     + $"| #{st.ShardId.ToString().PadBoth(3)} "
                                                     + $"| {timeDiff:mm\\:ss} "
                                                     + $"| {st.GuildCount.ToString().PadBoth(maxGuildCountLength)} `";
                                          })
                                          .ToArray();
            await ctx.SendPaginatedConfirmAsync(page,
                curPage =>
                {
                    var str = string.Join("\n", allShardStrings.Skip(25 * curPage).Take(25));

                    if (string.IsNullOrWhiteSpace(str))
                        str = GetText(strs.no_shards_on_page);

                    return new EmbedBuilder().WithOkColor().WithDescription($"{status}\n\n{str}");
                },
                allShardStrings.Length,
                25);
        }

        private static string ConnectionStateToEmoji(ShardStatus status)
        {
            var timeDiff = DateTime.UtcNow - status.LastUpdate;
            return status.ConnectionState switch
            {
                ConnectionState.Disconnected => "🔻",
                _ when timeDiff > TimeSpan.FromSeconds(30) => " ❗ ",
                ConnectionState.Connected => "✅",
                _ => " ⏳"
            };
        }

        [Cmd]
        [OwnerOnly]
        public async Task RestartShard(int shardId)
        {
            var success = _coord.RestartShard(shardId);
            if (success)
                await Response().Confirm(strs.shard_reconnecting(Format.Bold("#" + shardId))).SendAsync();
            else
                await Response().Error(strs.no_shard_id).SendAsync();
        }

        [Cmd]
        [OwnerOnly]
        public Task Leave([Leftover] string guildStr)
            => _service.LeaveGuild(guildStr);

        [Cmd]
        [OwnerOnly]
        public async Task DeleteEmptyServers()
        {
            await ctx.Channel.TriggerTypingAsync();

            var toLeave = _client.Guilds
                                 .Where(s => s.MemberCount == 1 && s.Users.Count == 1)
                                 .ToList();

            foreach (var server in toLeave)
            {
                try
                {
                    await server.DeleteAsync();
                    Log.Information("Deleted server {ServerName} [{ServerId}]",
                        server.Name,
                        server.Id);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex,
                        "Error leaving server {ServerName} [{ServerId}]",
                        server.Name,
                        server.Id);
                }
            }

            await Response().Confirm(strs.deleted_x_servers(toLeave.Count)).SendAsync();
        }

        [Cmd]
        [OwnerOnly]
        public async Task Die(bool graceful = false)
        {
            try
            {
                await _client.SetStatusAsync(UserStatus.Invisible);
                _ = _client.StopAsync();
                await Response().Confirm(strs.shutting_down).SendAsync();
            }
            catch
            {
                // ignored
            }

            await Task.Delay(2000);
            _coord.Die(graceful);
        }

        [Cmd]
        [OwnerOnly]
        public async Task Restart()
        {
            var success = _coord.RestartBot();
            if (!success)
            {
                await Response().Error(strs.restart_fail).SendAsync();
                return;
            }

            try { await Response().Confirm(strs.restarting).SendAsync(); }
            catch { }
        }

        [Cmd]
        [OwnerOnly]
        public async Task SetName([Leftover] string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                return;

            try
            {
                await _client.CurrentUser.ModifyAsync(u => u.Username = newName);
            }
            catch (RateLimitedException)
            {
                Log.Warning("You've been ratelimited. Wait 2 hours to change your name");
            }

            await Response().Confirm(strs.bot_name(Format.Bold(newName))).SendAsync();
        }

        [Cmd]
        [UserPerm(GuildPerm.ManageNicknames)]
        [BotPerm(GuildPerm.ChangeNickname)]
        [Priority(0)]
        public async Task SetNick([Leftover] string newNick = null)
        {
            if (string.IsNullOrWhiteSpace(newNick))
                return;
            var curUser = await ctx.Guild.GetCurrentUserAsync();
            await curUser.ModifyAsync(u => u.Nickname = newNick);

            await Response().Confirm(strs.bot_nick(Format.Bold(newNick) ?? "-")).SendAsync();
        }

        [Cmd]
        [BotPerm(GuildPerm.ManageNicknames)]
        [UserPerm(GuildPerm.ManageNicknames)]
        [Priority(1)]
        public async Task SetNick(IGuildUser gu, [Leftover] string newNick = null)
        {
            var sg = (SocketGuild)ctx.Guild;
            if (sg.OwnerId == gu.Id
                || gu.GetRoles().Max(r => r.Position) >= sg.CurrentUser.GetRoles().Max(r => r.Position))
            {
                await Response().Error(strs.insuf_perms_i).SendAsync();
                return;
            }

            await gu.ModifyAsync(u => u.Nickname = newNick);

            await Response()
                  .Confirm(strs.user_nick(Format.Bold(gu.ToString()), Format.Bold(newNick) ?? "-"))
                  .SendAsync();
        }

        [Cmd]
        [OwnerOnly]
        public async Task SetStatus([Leftover] SettableUserStatus status)
        {
            await _client.SetStatusAsync(SettableUserStatusToUserStatus(status));

            await Response().Confirm(strs.bot_status(Format.Bold(status.ToString()))).SendAsync();
        }

        [Cmd]
        [OwnerOnly]
        public async Task SetAvatar([Leftover] string img = null)
        {
            var success = await _service.SetAvatar(img);

            if (success)
                await Response().Confirm(strs.set_avatar).SendAsync();
        }

        [Cmd]
        [OwnerOnly]
        public async Task SetBanner([Leftover] string img = null)
        {
            var success = await _service.SetBanner(img);

            if (success)
                await Response().Confirm(strs.set_banner).SendAsync();
        }

        [Cmd]
        [OwnerOnly]
        public async Task SetGame(ActivityType type, [Leftover] string game = null)
        {
            // var rep = new ReplacementBuilder().WithDefault(Context).Build();

            var repCtx = new ReplacementContext(ctx);
            await _service.SetGameAsync(game is null ? game : await repSvc.ReplaceAsync(game, repCtx), type);

            await Response().Confirm(strs.set_game).SendAsync();
        }

        [Cmd]
        [OwnerOnly]
        public async Task SetStream(string url, [Leftover] string name = null)
        {
            name ??= "";

            await _service.SetStreamAsync(name, url);

            await Response().Confirm(strs.set_stream).SendAsync();
        }

        [Cmd]
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

            // var repSvc = new ReplacementBuilder().WithDefault(Context).Build();
            var repCtx = new ReplacementContext(Context);

            if (ids[1].ToUpperInvariant().StartsWith("C:", StringComparison.InvariantCulture))
            {
                var cid = ulong.Parse(ids[1][2..]);
                var ch = server.TextChannels.FirstOrDefault(c => c.Id == cid);
                if (ch is null)
                    return;

                text = await repSvc.ReplaceAsync(text, repCtx);
                await ch.SendAsync(text);
            }
            else if (ids[1].ToUpperInvariant().StartsWith("U:", StringComparison.InvariantCulture))
            {
                var uid = ulong.Parse(ids[1][2..]);
                var user = server.Users.FirstOrDefault(u => u.Id == uid);
                if (user is null)
                    return;

                var ch = await user.CreateDMChannelAsync();
                text = await repSvc.ReplaceAsync(text, repCtx);
                await ch.SendAsync(text);
            }
            else
            {
                await Response().Error(strs.invalid_format).SendAsync();
                return;
            }

            await Response().Confirm(strs.message_sent).SendAsync();
        }

        [Cmd]
        [OwnerOnly]
        public async Task StringsReload()
        {
            _strings.Reload();
            await _medusaLoader.ReloadStrings();
            await Response().Confirm(strs.bot_strings_reloaded).SendAsync();
        }

        [Cmd]
        [OwnerOnly]
        public async Task CoordReload()
        {
            await _coord.Reload();
            await ctx.OkAsync();
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
    }
}