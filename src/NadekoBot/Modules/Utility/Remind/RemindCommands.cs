#nullable disable
using Humanizer.Localisation;
using NadekoBot.Db;
using NadekoBot.Modules.Administration.Services;
using NadekoBot.Modules.Utility.Services;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Utility;

public partial class Utility
{
    [Group]
    public partial class RemindCommands : NadekoModule<RemindService>
    {
        public enum MeOrHere
        {
            Me,
            Here
        }

        public enum Server
        {
            Server = int.MinValue,
            Srvr = int.MinValue,
            Serv = int.MinValue,
            S = int.MinValue
        }

        private readonly DbService _db;
        private readonly GuildTimezoneService _tz;

        public RemindCommands(DbService db, GuildTimezoneService tz)
        {
            _db = db;
            _tz = tz;
        }

        [Cmd]
        [Priority(1)]
        public async partial Task Remind(MeOrHere meorhere, [Leftover] string remindString)
        {
            if (!_service.TryParseRemindMessage(remindString, out var remindData))
            {
                await ReplyErrorLocalizedAsync(strs.remind_invalid);
                return;
            }

            ulong target;
            target = meorhere == MeOrHere.Me ? ctx.User.Id : ctx.Channel.Id;
            if (!await RemindInternal(target,
                    meorhere == MeOrHere.Me || ctx.Guild is null,
                    remindData.Time,
                    remindData.What))
                await ReplyErrorLocalizedAsync(strs.remind_too_long);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        [Priority(0)]
        public async partial Task Remind(ITextChannel channel, [Leftover] string remindString)
        {
            var perms = ((IGuildUser)ctx.User).GetPermissions(channel);
            if (!perms.SendMessages || !perms.ViewChannel)
            {
                await ReplyErrorLocalizedAsync(strs.cant_read_or_send);
                return;
            }

            if (!_service.TryParseRemindMessage(remindString, out var remindData))
            {
                await ReplyErrorLocalizedAsync(strs.remind_invalid);
                return;
            }


            if (!await RemindInternal(channel.Id, false, remindData.Time, remindData.What))
                await ReplyErrorLocalizedAsync(strs.remind_too_long);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [Priority(0)]
        public partial Task RemindList(Server _, int page = 1)
            => RemindListInternal(page, true);

        [Cmd]
        [Priority(1)]
        public partial Task RemindList(int page = 1)
            => RemindListInternal(page, false);

        private async Task RemindListInternal(int page, bool isServer)
        {
            if (--page < 0)
                return;

            var embed = _eb.Create()
                           .WithOkColor()
                           .WithTitle(GetText(isServer ? strs.reminder_server_list : strs.reminder_list));

            List<Reminder> rems;
            await using (var uow = _db.GetDbContext())
            {
                if (isServer)
                    rems = uow.Reminders.RemindersForServer(ctx.Guild.Id, page).ToList();
                else
                    rems = uow.Reminders.RemindersFor(ctx.User.Id, page).ToList();
            }

            if (rems.Any())
            {
                var i = 0;
                foreach (var rem in rems)
                {
                    var when = rem.When;
                    var diff = when - DateTime.UtcNow;
                    embed.AddField(
                        $"#{++i + (page * 10)} {rem.When:HH:mm yyyy-MM-dd} UTC "
                        + $"(in {diff.Humanize(2, minUnit: TimeUnit.Minute, culture: Culture)})",
                        $@"`Target:` {(rem.IsPrivate ? "DM" : "Channel")}
`TargetId:` {rem.ChannelId}
`Message:` {rem.Message?.TrimTo(50)}");
                }
            }
            else
                embed.WithDescription(GetText(strs.reminders_none));

            embed.AddPaginatedFooter(page + 1, null);
            await ctx.Channel.EmbedAsync(embed);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [Priority(0)]
        public partial Task RemindDelete(Server _, int index)
            => RemindDelete(index, true);

        [Cmd]
        [Priority(1)]
        public partial Task RemindDelete(int index)
            => RemindDelete(index, false);

        private async Task RemindDelete(int index, bool isServer)
        {
            if (--index < 0)
                return;

            Reminder rem = null;
            await using (var uow = _db.GetDbContext())
            {
                var rems = isServer
                    ? uow.Reminders.RemindersForServer(ctx.Guild.Id, index / 10).ToList()
                    : uow.Reminders.RemindersFor(ctx.User.Id, index / 10).ToList();

                var pageIndex = index % 10;
                if (rems.Count > pageIndex)
                {
                    rem = rems[pageIndex];
                    uow.Reminders.Remove(rem);
                    uow.SaveChanges();
                }
            }

            if (rem is null)
                await ReplyErrorLocalizedAsync(strs.reminder_not_exist);
            else
                await ReplyConfirmLocalizedAsync(strs.reminder_deleted(index + 1));
        }

        private async Task<bool> RemindInternal(
            ulong targetId,
            bool isPrivate,
            TimeSpan ts,
            string message)
        {
            var time = DateTime.UtcNow + ts;

            if (ts > TimeSpan.FromDays(60))
                return false;

            if (ctx.Guild is not null)
            {
                var perms = ((IGuildUser)ctx.User).GetPermissions((IGuildChannel)ctx.Channel);
                if (!perms.MentionEveryone)
                    message = message.SanitizeAllMentions();
            }

            var rem = new Reminder
            {
                ChannelId = targetId,
                IsPrivate = isPrivate,
                When = time,
                Message = message,
                UserId = ctx.User.Id,
                ServerId = ctx.Guild?.Id ?? 0
            };

            await using (var uow = _db.GetDbContext())
            {
                uow.Reminders.Add(rem);
                await uow.SaveChangesAsync();
            }

            var gTime = ctx.Guild is null ? time : TimeZoneInfo.ConvertTime(time, _tz.GetTimeZoneOrUtc(ctx.Guild.Id));
            try
            {
                await SendConfirmAsync("⏰ "
                                       + GetText(strs.remind(
                                           Format.Bold(!isPrivate ? $"<#{targetId}>" : ctx.User.Username),
                                           Format.Bold(message),
                                           ts.Humanize(3, minUnit: TimeUnit.Second, culture: Culture),
                                           gTime,
                                           gTime)));
            }
            catch
            {
            }

            return true;
        }
    }
}