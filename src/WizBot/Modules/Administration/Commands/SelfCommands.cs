using Discord;
using Discord.Commands;
using WizBot.Attributes;
using WizBot.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace WizBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        class SelfCommands : ModuleBase
        {
            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task Leave([Remainder] string guildStr)
            {
                guildStr = guildStr.Trim().ToUpperInvariant();
                var server = WizBot.Client.GetGuilds().FirstOrDefault(g => g.Id.ToString() == guildStr) ??
                    WizBot.Client.GetGuilds().FirstOrDefault(g => g.Name.Trim().ToUpperInvariant() == guildStr);

                if (server == null)
                {
                    await Context.Channel.SendErrorAsync("⚠️ Cannot find that server").ConfigureAwait(false);
                    return;
                }
                if (server.OwnerId != WizBot.Client.CurrentUser.Id)
                {
                    await server.LeaveAsync().ConfigureAwait(false);
                    await Context.Channel.SendConfirmAsync("✅ Left server " + server.Name).ConfigureAwait(false);
                }
                else
                {
                    await server.DeleteAsync().ConfigureAwait(false);
                    await Context.Channel.SendConfirmAsync("Deleted server " + server.Name).ConfigureAwait(false);
                }
            }


            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task Die()
            {
                try { await Context.Channel.SendConfirmAsync("ℹ️ **Shutting down.**").ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
                await Task.Delay(2000).ConfigureAwait(false);
                Environment.Exit(0);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task SetName([Remainder] string newName)
            {
                if (string.IsNullOrWhiteSpace(newName))
                    return;

                await WizBot.Client.CurrentUser.ModifyAsync(u => u.Username = newName).ConfigureAwait(false);

                await Context.Channel.SendConfirmAsync($"Bot name changed to **{newName}**").ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task SetStatus([Remainder] SettableUserStatus status)
            {
                await WizBot.Client.SetStatusAsync(SettableUserStatusToUserStatus(status)).ConfigureAwait(false);

                await Context.Channel.SendConfirmAsync($"Bot status changed to **{status}**").ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task SetAvatar([Remainder] string img = null)
            {
                if (string.IsNullOrWhiteSpace(img))
                    return;

                using (var http = new HttpClient())
                {
                    using (var sr = await http.GetStreamAsync(img))
                    {
                        var imgStream = new MemoryStream();
                        await sr.CopyToAsync(imgStream);
                        imgStream.Position = 0;

                        await WizBot.Client.CurrentUser.ModifyAsync(u => u.Avatar = new Image(imgStream)).ConfigureAwait(false);
                    }
                }

                await Context.Channel.SendConfirmAsync("🆒 **New avatar set.**").ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task SetGame([Remainder] string game = null)
            {
                await WizBot.Client.SetGameAsync(game).ConfigureAwait(false);

                await Context.Channel.SendConfirmAsync("👾 **New game set.**").ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task SetStream(string url, [Remainder] string name = null)
            {
                name = name ?? "";

                await WizBot.Client.SetGameAsync(name, url, StreamType.Twitch).ConfigureAwait(false);

                await Context.Channel.SendConfirmAsync("ℹ️ **New stream set.**").ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task Send(string where, [Remainder] string msg = null)
            {
                if (string.IsNullOrWhiteSpace(msg))
                    return;

                var ids = where.Split('|');
                if (ids.Length != 2)
                    return;
                var sid = ulong.Parse(ids[0]);
                var server = WizBot.Client.GetGuilds().Where(s => s.Id == sid).FirstOrDefault();

                if (server == null)
                    return;

                if (ids[1].ToUpperInvariant().StartsWith("C:"))
                {
                    var cid = ulong.Parse(ids[1].Substring(2));
                    var ch = server.TextChannels.Where(c => c.Id == cid).FirstOrDefault();
                    if (ch == null)
                    {
                        return;
                    }
                    await ch.SendMessageAsync(msg).ConfigureAwait(false);
                }
                else if (ids[1].ToUpperInvariant().StartsWith("U:"))
                {
                    var uid = ulong.Parse(ids[1].Substring(2));
                    var user = server.Users.Where(u => u.Id == uid).FirstOrDefault();
                    if (user == null)
                    {
                        return;
                    }
                    await user.SendMessageAsync(msg).ConfigureAwait(false);
                }
                else
                {
                    await Context.Channel.SendErrorAsync("⚠️ Invalid format.").ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task Announce([Remainder] string message)
            {
                var channels = WizBot.Client.GetGuilds().Select(g => g.DefaultChannel).ToArray();
                if (channels == null)
                    return;
                await Task.WhenAll(channels.Where(c => c != null).Select(c => c.SendConfirmAsync($"🆕 Message from {Context.User} `[Bot Owner]`:", message)))
                        .ConfigureAwait(false);

                await Context.Channel.SendConfirmAsync("🆗").ConfigureAwait(false);
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
