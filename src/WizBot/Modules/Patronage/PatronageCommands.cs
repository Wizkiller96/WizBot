﻿using WizBot.Modules.Patronage;

namespace WizBot.Modules.Help;

public partial class Help
{
    [OnlyPublicBot]
    public partial class Patronage : WizBotModule
    {
        private readonly PatronageService _service;
        private readonly PatronageConfig _pConf;

        public Patronage(PatronageService service, PatronageConfig pConf)
        {
            _service = service;
            _pConf = pConf;
        }

        [Cmd]
        [Priority(2)]
        public Task Patron()
            => InternalPatron(ctx.User);

        [Cmd]
        [Priority(0)]
        [OwnerOnly]
        public Task Patron(IUser user)
            => InternalPatron(user);

        [Cmd]
        [Priority(0)]
        [OwnerOnly]
        public async Task PatronMessage(PatronTier tierAndHigher, string message)
        {
            _ = ctx.Channel.TriggerTypingAsync();
            var result = await _service.SendMessageToPatronsAsync(tierAndHigher, message);

            await Response()
                  .Confirm(strs.patron_msg_sent(
                      Format.Code(tierAndHigher.ToString()),
                      Format.Bold(result.Success.ToString()),
                      Format.Bold(result.Failed.ToString())))
                  .SendAsync();
        }

        // [OwnerOnly]
        // public async Task PatronGift(IUser user, int amount)
        // {
        //     // i can't figure out a good way to gift more than one month at the moment.
        //
        //     if (amount < 1)
        //         return;
        //     
        //     var patron = _service.GiftPatronAsync(user, amount);
        //
        //     var eb = _sender.CreateEmbed();
        //
        //     await Response().Embed(eb.WithDescription($"Added **{days}** days of Patron benefits to {user.Mention}!")
        //                                    .AddField("Tier", Format.Bold(patron.Tier.ToString()), true)
        //                                    .AddField("Amount", $"**{patron.Amount / 100.0f:N1}$**", true)
        //                                    .AddField("Until", TimestampTag.FromDateTime(patron.ValidThru.AddDays(1)))).SendAsync();
        //     
        //
        // }

        private async Task InternalPatron(IUser user)
        {
            if (!_pConf.Data.IsEnabled)
            {
                await Response().Error(strs.patron_not_enabled).SendAsync();
                return;
            }

            var patron = await _service.GetPatronAsync(user.Id);
            var quotaStats = await _service.GetUserQuotaStatistic(user.Id);

            var eb = _sender.CreateEmbed()
                            .WithAuthor(user)
                            .WithTitle(GetText(strs.patron_info))
                            .WithOkColor();

            if (quotaStats.Commands.Count == 0
                && quotaStats.Groups.Count == 0
                && quotaStats.Modules.Count == 0)
            {
                eb.WithDescription(GetText(strs.no_quota_found));
            }
            else
            {
                eb.AddField(GetText(strs.tier), Format.Bold(patron.Tier.ToFullName()), true)
                  .AddField(GetText(strs.pledge), $"**{patron.Amount / 100.0f:N1}$**", true);

                if (patron.Tier != PatronTier.None)
                    eb.AddField(GetText(strs.expires),
                        patron.ValidThru.AddDays(1).ToShortAndRelativeTimestampTag(),
                        true);

                eb.AddField(GetText(strs.quotas), "⁣", false);

                if (quotaStats.Commands.Count > 0)
                {
                    var text = GetQuotaList(quotaStats.Commands);
                    if (!string.IsNullOrWhiteSpace(text))
                        eb.AddField(GetText(strs.commands), text, true);
                }

                if (quotaStats.Groups.Count > 0)
                {
                    var text = GetQuotaList(quotaStats.Groups);
                    if (!string.IsNullOrWhiteSpace(text))
                        eb.AddField(GetText(strs.groups), text, true);
                }

                if (quotaStats.Modules.Count > 0)
                {
                    var text = GetQuotaList(quotaStats.Modules);
                    if (!string.IsNullOrWhiteSpace(text))
                        eb.AddField(GetText(strs.modules), text, true);
                }
            }


            try
            {
                await Response().User(ctx.User).Embed(eb).SendAsync();
                _ = ctx.OkAsync();
            }
            catch
            {
                await Response().Error(strs.cant_dm).SendAsync();
            }
        }

        private string GetQuotaList(IReadOnlyDictionary<string, FeatureQuotaStats> featureQuotaStats)
        {
            var text = string.Empty;
            foreach (var (key, q) in featureQuotaStats)
            {
                text += $"\n⁣\t`{key}`\n";
                if (q.Hourly != default)
                    text += $"⁣ ⁣ {GetEmoji(q.Hourly)} {q.Hourly.Cur}/{q.Hourly.Max} per hour\n";
                if (q.Daily != default)
                    text += $"⁣ ⁣ {GetEmoji(q.Daily)} {q.Daily.Cur}/{q.Daily.Max} per day\n";
                if (q.Monthly != default)
                    text += $"⁣ ⁣ {GetEmoji(q.Monthly)} {q.Monthly.Cur}/{q.Monthly.Max} per month\n";
            }

            return text;
        }

        private string GetEmoji((uint Cur, uint Max) limit)
            => limit.Cur < limit.Max
                ? "✅"
                : "⚠️";
    }
}