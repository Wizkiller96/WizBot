#nullable disable
using NadekoBot.Common.TypeReaders.Models;
using NadekoBot.Modules.Administration.Services;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class ProtectionCommands : NadekoModule<ProtectionService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async partial Task AntiAlt()
        {
            if (await _service.TryStopAntiAlt(ctx.Guild.Id))
            {
                await ReplyConfirmLocalizedAsync(strs.prot_disable("Anti-Alt"));
                return;
            }

            await ReplyConfirmLocalizedAsync(strs.protection_not_running("Anti-Alt"));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async partial Task AntiAlt(
            StoopidTime minAge,
            PunishmentAction action,
            [Leftover] StoopidTime punishTime = null)
        {
            var minAgeMinutes = (int)minAge.Time.TotalMinutes;
            var punishTimeMinutes = (int?)punishTime?.Time.TotalMinutes ?? 0;

            if (minAgeMinutes < 1 || punishTimeMinutes < 0)
                return;

            await _service.StartAntiAltAsync(ctx.Guild.Id,
                minAgeMinutes,
                action,
                (int?)punishTime?.Time.TotalMinutes ?? 0);

            await ctx.OkAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async partial Task AntiAlt(StoopidTime minAge, PunishmentAction action, [Leftover] IRole role)
        {
            var minAgeMinutes = (int)minAge.Time.TotalMinutes;

            if (minAgeMinutes < 1)
                return;

            await _service.StartAntiAltAsync(ctx.Guild.Id, minAgeMinutes, action, roleId: role.Id);

            await ctx.OkAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public partial Task AntiRaid()
        {
            if (_service.TryStopAntiRaid(ctx.Guild.Id))
                return ReplyConfirmLocalizedAsync(strs.prot_disable("Anti-Raid"));
            return ReplyPendingLocalizedAsync(strs.protection_not_running("Anti-Raid"));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [Priority(1)]
        public partial Task AntiRaid(
            int userThreshold,
            int seconds,
            PunishmentAction action,
            [Leftover] StoopidTime punishTime)
            => InternalAntiRaid(userThreshold, seconds, action, punishTime);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [Priority(2)]
        public partial Task AntiRaid(int userThreshold, int seconds, PunishmentAction action)
            => InternalAntiRaid(userThreshold, seconds, action);

        private async Task InternalAntiRaid(
            int userThreshold,
            int seconds = 10,
            PunishmentAction action = PunishmentAction.Mute,
            StoopidTime punishTime = null)
        {
            if (action == PunishmentAction.AddRole)
            {
                await ReplyErrorLocalizedAsync(strs.punishment_unsupported(action));
                return;
            }

            if (userThreshold is < 2 or > 30)
            {
                await ReplyErrorLocalizedAsync(strs.raid_cnt(2, 30));
                return;
            }

            if (seconds is < 2 or > 300)
            {
                await ReplyErrorLocalizedAsync(strs.raid_time(2, 300));
                return;
            }

            if (punishTime is not null)
            {
                if (!_service.IsDurationAllowed(action))
                    await ReplyErrorLocalizedAsync(strs.prot_cant_use_time);
            }

            var time = (int?)punishTime?.Time.TotalMinutes ?? 0;
            if (time is < 0 or > 60 * 24)
                return;

            var stats = await _service.StartAntiRaidAsync(ctx.Guild.Id, userThreshold, seconds, action, time);

            if (stats is null)
                return;

            await SendConfirmAsync(GetText(strs.prot_enable("Anti-Raid")),
                $"{ctx.User.Mention} {GetAntiRaidString(stats)}");
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public partial Task AntiSpam()
        {
            if (_service.TryStopAntiSpam(ctx.Guild.Id))
                return ReplyConfirmLocalizedAsync(strs.prot_disable("Anti-Spam"));
            return ReplyPendingLocalizedAsync(strs.protection_not_running("Anti-Spam"));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [Priority(0)]
        public partial Task AntiSpam(int messageCount, PunishmentAction action, [Leftover] IRole role)
        {
            if (action != PunishmentAction.AddRole)
                return Task.CompletedTask;

            return InternalAntiSpam(messageCount, action, null, role);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [Priority(1)]
        public partial Task AntiSpam(int messageCount, PunishmentAction action, [Leftover] StoopidTime punishTime)
            => InternalAntiSpam(messageCount, action, punishTime);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [Priority(2)]
        public partial Task AntiSpam(int messageCount, PunishmentAction action)
            => InternalAntiSpam(messageCount, action);

        private async Task InternalAntiSpam(
            int messageCount,
            PunishmentAction action,
            StoopidTime timeData = null,
            IRole role = null)
        {
            if (messageCount is < 2 or > 10)
                return;

            if (timeData is not null)
            {
                if (!_service.IsDurationAllowed(action))
                    await ReplyErrorLocalizedAsync(strs.prot_cant_use_time);
            }

            var time = (int?)timeData?.Time.TotalMinutes ?? 0;
            if (time is < 0 or > 60 * 24)
                return;

            var stats = await _service.StartAntiSpamAsync(ctx.Guild.Id, messageCount, action, time, role?.Id);

            await SendConfirmAsync(GetText(strs.prot_enable("Anti-Spam")),
                $"{ctx.User.Mention} {GetAntiSpamString(stats)}");
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async partial Task AntispamIgnore()
        {
            var added = await _service.AntiSpamIgnoreAsync(ctx.Guild.Id, ctx.Channel.Id);

            if (added is null)
            {
                await ReplyErrorLocalizedAsync(strs.protection_not_running("Anti-Spam"));
                return;
            }

            if (added.Value)
                await ReplyConfirmLocalizedAsync(strs.spam_ignore("Anti-Spam"));
            else
                await ReplyConfirmLocalizedAsync(strs.spam_not_ignore("Anti-Spam"));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task AntiList()
        {
            var (spam, raid, alt) = _service.GetAntiStats(ctx.Guild.Id);

            if (spam is null && raid is null && alt is null)
            {
                await ReplyConfirmLocalizedAsync(strs.prot_none);
                return;
            }

            var embed = _eb.Create().WithOkColor().WithTitle(GetText(strs.prot_active));

            if (spam is not null)
                embed.AddField("Anti-Spam", GetAntiSpamString(spam).TrimTo(1024), true);

            if (raid is not null)
                embed.AddField("Anti-Raid", GetAntiRaidString(raid).TrimTo(1024), true);

            if (alt is not null)
                embed.AddField("Anti-Alt", GetAntiAltString(alt), true);

            await ctx.Channel.EmbedAsync(embed);
        }

        private string GetAntiAltString(AntiAltStats alt)
            => GetText(strs.anti_alt_status(Format.Bold(alt.MinAge.ToString(@"dd\d\ hh\h\ mm\m\ ")),
                Format.Bold(alt.Action.ToString()),
                Format.Bold(alt.Counter.ToString())));

        private string GetAntiSpamString(AntiSpamStats stats)
        {
            var settings = stats.AntiSpamSettings;
            var ignoredString = string.Join(", ", settings.IgnoredChannels.Select(c => $"<#{c.ChannelId}>"));

            if (string.IsNullOrWhiteSpace(ignoredString))
                ignoredString = "none";

            var add = string.Empty;
            if (settings.MuteTime > 0)
                add = $" ({TimeSpan.FromMinutes(settings.MuteTime):hh\\hmm\\m})";

            return GetText(strs.spam_stats(Format.Bold(settings.MessageThreshold.ToString()),
                Format.Bold(settings.Action + add),
                ignoredString));
        }

        private string GetAntiRaidString(AntiRaidStats stats)
        {
            var actionString = Format.Bold(stats.AntiRaidSettings.Action.ToString());

            if (stats.AntiRaidSettings.PunishDuration > 0)
                actionString += $" **({TimeSpan.FromMinutes(stats.AntiRaidSettings.PunishDuration):hh\\hmm\\m})**";

            return GetText(strs.raid_stats(Format.Bold(stats.AntiRaidSettings.UserThreshold.ToString()),
                Format.Bold(stats.AntiRaidSettings.Seconds.ToString()),
                actionString));
        }
    }
}