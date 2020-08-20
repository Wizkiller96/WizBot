﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using WizBot.Common;
using WizBot.Common.Attributes;
using WizBot.Core.Common;
using WizBot.Core.Modules.Gambling.Common;
using WizBot.Core.Services;
using WizBot.Core.Services.Database;
using WizBot.Core.Services.Database.Models;
using WizBot.Extensions;
using WizBot.Modules.Gambling.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace WizBot.Modules.Gambling
{
    public partial class Gambling : GamblingTopLevelModule<GamblingService>
    {
        private readonly DbService _db;
        private readonly ICurrencyService _cs;
        private readonly IDataCache _cache;
        private readonly DiscordSocketClient _client;
        private readonly IBotConfigProvider _bc;
        private readonly NumberFormatInfo _enUsCulture;
        private readonly DownloadTracker _tracker;

        private string CurrencyName => Bc.BotConfig.CurrencyName;
        private string CurrencyPluralName => Bc.BotConfig.CurrencyPluralName;
        private string CurrencySign => Bc.BotConfig.CurrencySign;

        public Gambling(DbService db, ICurrencyService currency,
            IDataCache cache, DiscordSocketClient client, IBotConfigProvider bc,
            DownloadTracker tracker)
        {
            _db = db;
            _cs = currency;
            _cache = cache;
            _client = client;
            _bc = bc;
            _enUsCulture = new CultureInfo("en-US", false).NumberFormat;
            _enUsCulture.NumberDecimalDigits = 0;
            _enUsCulture.NumberGroupSeparator = " ";
            _tracker = tracker;
        }

        private string n(long cur) => cur.ToString("N", _enUsCulture);

        public string GetCurrency(ulong id)
        {
            using (var uow = _db.GetDbContext())
            {
                return n(uow.DiscordUsers.GetUserCurrency(id));
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task Economy()
        {
            var ec = _service.GetEconomy();
            decimal onePercent = 0;
            if (ec.Cash > 0)
            {
                onePercent = ec.OnePercent / (ec.Cash - ec.Bot); // This stops the top 1% from owning more than 100% of the money
                // [21:03] Bob Page: Kinda remids me of US economy
            }
            var embed = new EmbedBuilder()
                .WithTitle(GetText("economy_state"))
                .AddField(GetText("currency_owned"), ((BigInteger)ec.Cash) + _bc.BotConfig.CurrencySign)
                .AddField(GetText("currency_one_percent"), (onePercent * 100).ToString("F2") + "%")
                .AddField(GetText("currency_planted"), ((BigInteger)ec.Planted) + _bc.BotConfig.CurrencySign)
                .AddField(GetText("owned_waifus_total"), ((BigInteger)ec.Waifus) + _bc.BotConfig.CurrencySign)
                .AddField(GetText("bot_currency"), ec.Bot + _bc.BotConfig.CurrencySign)
                .AddField(GetText("total"), ((BigInteger)(ec.Cash + ec.Bot + ec.Planted + ec.Waifus)).ToString("N", _enUsCulture) + _bc.BotConfig.CurrencySign)
                .WithOkColor();

            await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task Timely()
        {
            var val = Bc.BotConfig.TimelyCurrency;
            var period = Bc.BotConfig.TimelyCurrencyPeriod;
            if (val <= 0 || period <= 0)
            {
                await ReplyErrorLocalizedAsync("timely_none").ConfigureAwait(false);
                return;
            }

            TimeSpan? rem;
            if ((rem = _cache.AddTimelyClaim(ctx.User.Id, period)) != null)
            {
                await ReplyErrorLocalizedAsync("timely_already_claimed", rem?.ToString(@"dd\d\ hh\h\ mm\m\ ss\s")).ConfigureAwait(false);
                return;
            }

            await _cs.AddAsync(ctx.User.Id, "Timely claim", val).ConfigureAwait(false);

            await ReplyConfirmLocalizedAsync("timely", n(val) + Bc.BotConfig.CurrencySign, period).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [OwnerOnly]
        public async Task TimelyReset()
        {
            _cache.RemoveAllTimelyClaims();
            await ReplyConfirmLocalizedAsync("timely_reset").ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [OwnerOnly]
        public async Task TimelySet(int num, int period = 24)
        {
            if (num < 0 || period < 0)
                return;
            using (var uow = _db.GetDbContext())
            {
                var bc = uow.BotConfig.GetOrCreate(set => set);
                _bc.BotConfig.TimelyCurrency = bc.TimelyCurrency = num;
                _bc.BotConfig.TimelyCurrencyPeriod = bc.TimelyCurrencyPeriod = period;
                uow.SaveChanges();
            }
            if (num == 0)
                await ReplyConfirmLocalizedAsync("timely_set_none").ConfigureAwait(false);
            else
                await ReplyConfirmLocalizedAsync("timely_set", Format.Bold(n(num) + Bc.BotConfig.CurrencySign), Format.Bold(period.ToString())).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Raffle([Leftover] IRole role = null)
        {
            role = role ?? ctx.Guild.EveryoneRole;

            var members = (await role.GetMembersAsync().ConfigureAwait(false)).Where(u => u.Status != UserStatus.Offline);
            var membersArray = members as IUser[] ?? members.ToArray();
            if (membersArray.Length == 0)
            {
                return;
            }
            var usr = membersArray[new WizBotRandom().Next(0, membersArray.Length)];
            await ctx.Channel.SendConfirmAsync("🎟 " + GetText("raffled_user"), $"**{usr.Username}#{usr.Discriminator}**", footer: $"ID: {usr.Id}").ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task RaffleAny([Leftover] IRole role = null)
        {
            role = role ?? ctx.Guild.EveryoneRole;

            var members = (await role.GetMembersAsync().ConfigureAwait(false));
            var membersArray = members as IUser[] ?? members.ToArray();
            if (membersArray.Length == 0)
            {
                return;
            }
            var usr = membersArray[new WizBotRandom().Next(0, membersArray.Length)];
            await ctx.Channel.SendConfirmAsync("🎟 " + GetText("raffled_user"), $"**{usr.Username}#{usr.Discriminator}**", footer: $"ID: {usr.Id}").ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [Priority(1)]
        public async Task Cash([Leftover] IUser user = null)
        {
            user = user ?? ctx.User;
            await ConfirmLocalizedAsync("has", Format.Bold(user.ToString()), $"{GetCurrency(user.Id)} {CurrencySign}").ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [Priority(2)]
        public Task CurrencyTransactions(int page = 1) =>
            InternalCurrencyTransactions(ctx.User.Id, page);

        [WizBotCommand, Usage, Description, Aliases]
        [AdminOnly]
        [Priority(0)]
        public Task CurrencyTransactions([Leftover] IUser usr) =>
            InternalCurrencyTransactions(usr.Id, 1);

        [WizBotCommand, Usage, Description, Aliases]
        [AdminOnly]
        [Priority(1)]
        public Task CurrencyTransactions(IUser usr, int page) =>
            InternalCurrencyTransactions(usr.Id, page);

        private async Task InternalCurrencyTransactions(ulong userId, int page)
        {
            if (--page < 0)
                return;

            var trs = new List<CurrencyTransaction>();
            using (var uow = _db.GetDbContext())
            {
                trs = uow.CurrencyTransactions.GetPageFor(userId, page);
            }

            var embed = new EmbedBuilder()
                .WithTitle(GetText("transactions",
                    ((SocketGuild)ctx.Guild)?.GetUser(userId)?.ToString() ?? $"{userId}"))
                .WithOkColor();

            var desc = "";
            foreach (var tr in trs)
            {
                var type = tr.Amount > 0 ? "🔵" : "🔴";
                var date = Format.Code($"〖{tr.DateAdded:HH:mm yyyy-MM-dd}〗");
                desc += $"\\{type} {date} {Format.Bold(n(tr.Amount))}\n\t{tr.Reason?.Trim()}\n";
            }

            embed.WithDescription(desc);
            embed.WithFooter(GetText("page", page + 1));
            await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [Priority(0)]
        public async Task Cash(ulong userId)
        {
            await ReplyConfirmLocalizedAsync("has", Format.Code(userId.ToString()), $"{GetCurrency(userId)} {CurrencySign}").ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public async Task Give(ShmartNumber amount, IGuildUser receiver, [Leftover] string msg = null)
        {
            if (amount <= 0 || ctx.User.Id == receiver.Id || receiver.IsBot)
                return;
            var success = await _cs.RemoveAsync((IGuildUser)ctx.User, $"Gift to {receiver.Username} ({receiver.Id}).", amount, false).ConfigureAwait(false);
            if (!success)
            {
                await ReplyErrorLocalizedAsync("not_enough", CurrencyPluralName).ConfigureAwait(false);
                return;
            }
            await _cs.AddAsync(receiver, $"Gift from {ctx.User.Username} ({ctx.User.Id}) - {msg}.", amount, true).ConfigureAwait(false);
            await ReplyConfirmLocalizedAsync("gifted", n(amount) + CurrencySign, Format.Bold(receiver.ToString()), msg)
                .ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public Task Give(ShmartNumber amount, [Leftover] IGuildUser receiver)
            => Give(amount, receiver, null);

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [AdminOnly]
        [Priority(0)]
        public Task Award(ShmartNumber amount, IGuildUser usr, [Leftover] string msg) =>
            Award(amount, usr.Id, msg);

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [AdminOnly]
        [Priority(1)]
        public Task Award(ShmartNumber amount, [Leftover] IGuildUser usr) =>
            Award(amount, usr.Id);

        [WizBotCommand, Usage, Description, Aliases]
        [AdminOnly]
        [Priority(2)]
        public async Task Award(ShmartNumber amount, ulong usrId, [Leftover] string msg = null)
        {
            if (amount <= 0)
                return;

            await _cs.AddAsync(usrId,
                $"Awarded by bot staff. ({ctx.User.Username}/{ctx.User.Id}) {(msg ?? "")}",
                amount,
                gamble: (ctx.Client.CurrentUser.Id != usrId)).ConfigureAwait(false);
            await ReplyConfirmLocalizedAsync("awarded", n(amount) + CurrencySign, $"<@{usrId}>").ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [AdminOnly]
        [Priority(2)]
        public async Task Award(ShmartNumber amount, [Leftover] IRole role)
        {
            var users = (await ctx.Guild.GetUsersAsync().ConfigureAwait(false))
                               .Where(u => u.GetRoles().Contains(role))
                               .ToList();

            await _cs.AddBulkAsync(users.Select(x => x.Id),
                users.Select(x => $"Awarded by bot staff to **{role.Name}** role. ({ctx.User.Username}/{ctx.User.Id})"),
                users.Select(x => amount.Value),
                gamble: true)
                .ConfigureAwait(false);

            await ReplyConfirmLocalizedAsync("mass_award",
                n(amount) + CurrencySign,
                Format.Bold(users.Count.ToString()),
                Format.Bold(role.Name)).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [AdminOnly]
        public async Task Take(ShmartNumber amount, [Leftover] IGuildUser user)
        {
            if (amount <= 0)
                return;

            if (await _cs.RemoveAsync(user, $"Taken by bot staff.({ctx.User.Username}/{ctx.User.Id})", amount,
                gamble: (ctx.Client.CurrentUser.Id != user.Id)).ConfigureAwait(false))
                await ReplyConfirmLocalizedAsync("take", n(amount) + CurrencySign, Format.Bold(user.ToString())).ConfigureAwait(false);
            else
                await ReplyErrorLocalizedAsync("take_fail", n(amount) + CurrencySign, Format.Bold(user.ToString()), CurrencyPluralName).ConfigureAwait(false);
        }


        [WizBotCommand, Usage, Description, Aliases]
        [AdminOnly]
        public async Task Take(ShmartNumber amount, [Leftover] ulong usrId)
        {
            if (amount <= 0)
                return;

            if (await _cs.RemoveAsync(usrId, $"Taken by bot staff.({ctx.User.Username}/{ctx.User.Id})", amount,
                gamble: (ctx.Client.CurrentUser.Id != usrId)).ConfigureAwait(false))
                await ReplyConfirmLocalizedAsync("take", amount + CurrencySign, $"<@{usrId}>").ConfigureAwait(false);
            else
                await ReplyErrorLocalizedAsync("take_fail", amount + CurrencySign, Format.Code(usrId.ToString()), CurrencyPluralName).ConfigureAwait(false);
        }

        private IUserMessage rdMsg = null;

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task RollDuel(IUser u)
        {
            if (ctx.User.Id == u.Id)
                return;

            //since the challenge is created by another user, we need to reverse the ids
            //if it gets removed, means challenge is accepted
            if (_service.Duels.TryRemove((ctx.User.Id, u.Id), out var game))
            {
                await game.StartGame().ConfigureAwait(false);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task RollDuel(ShmartNumber amount, IUser u)
        {
            if (ctx.User.Id == u.Id)
                return;

            if (amount <= 0)
                return;

            var embed = new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle(GetText("roll_duel"));

            var game = new RollDuelGame(_cs, _client.CurrentUser.Id, ctx.User.Id, u.Id, amount);
            //means challenge is just created
            if (_service.Duels.TryGetValue((ctx.User.Id, u.Id), out var other))
            {
                if (other.Amount != amount)
                {
                    await ReplyErrorLocalizedAsync("roll_duel_already_challenged").ConfigureAwait(false);
                }
                else
                {
                    await RollDuel(u).ConfigureAwait(false);
                }
                return;
            }
            if (_service.Duels.TryAdd((u.Id, ctx.User.Id), game))
            {
                game.OnGameTick += Game_OnGameTick;
                game.OnEnded += Game_OnEnded;

                await ReplyConfirmLocalizedAsync("roll_duel_challenge",
                    Format.Bold(ctx.User.ToString()),
                    Format.Bold(u.ToString()),
                    Format.Bold(amount + CurrencySign))
                        .ConfigureAwait(false);
            }

            async Task Game_OnGameTick(RollDuelGame arg)
            {
                var rolls = arg.Rolls.Last();
                embed.Description += $@"{Format.Bold(ctx.User.ToString())} rolled **{rolls.Item1}**
{Format.Bold(u.ToString())} rolled **{rolls.Item2}**
--
";

                if (rdMsg == null)
                {
                    rdMsg = await ctx.Channel.EmbedAsync(embed)
                        .ConfigureAwait(false);
                }
                else
                {
                    await rdMsg.ModifyAsync(x =>
                    {
                        x.Embed = embed.Build();
                    }).ConfigureAwait(false);
                }
            }

            async Task Game_OnEnded(RollDuelGame rdGame, RollDuelGame.Reason reason)
            {
                try
                {
                    if (reason == RollDuelGame.Reason.Normal)
                    {
                        var winner = rdGame.Winner == rdGame.P1
                            ? ctx.User
                            : u;
                        embed.Description += $"\n**{winner}** Won {n(((long)(rdGame.Amount * 2 * 0.98))) + CurrencySign}";
                        await rdMsg.ModifyAsync(x => x.Embed = embed.Build())
                            .ConfigureAwait(false);
                    }
                    else if (reason == RollDuelGame.Reason.Timeout)
                    {
                        await ReplyErrorLocalizedAsync("roll_duel_timeout").ConfigureAwait(false);
                    }
                    else if (reason == RollDuelGame.Reason.NoFunds)
                    {
                        await ReplyErrorLocalizedAsync("roll_duel_no_funds").ConfigureAwait(false);
                    }
                }
                finally
                {
                    _service.Duels.TryRemove((u.Id, ctx.User.Id), out var _);
                }
            }
        }

        private async Task InternallBetroll(long amount)
        {
            if (!await CheckBetMandatory(amount).ConfigureAwait(false))
                return;

            if (!await _cs.RemoveAsync(ctx.User, "Betroll Gamble", amount, false, gamble: true).ConfigureAwait(false))
            {
                await ReplyErrorLocalizedAsync("not_enough", CurrencyPluralName).ConfigureAwait(false);
                return;
            }

            var rnd = new WizBotRandom().Next(0, 101);
            var str = Format.Bold(ctx.User.ToString()) + Format.Code(GetText("roll", rnd));
            if (rnd < 67)
            {
                str += GetText("better_luck");
            }
            else
            {
                long win;
                if (rnd < 91)
                {
                    win = (long)(amount * Bc.BotConfig.Betroll67Multiplier);
                    str += GetText("br_win", n(win) + CurrencySign, 66);
                    await _cs.AddAsync(ctx.User, "Betroll Gamble",
                        win, false, gamble: true).ConfigureAwait(false);
                }
                else if (rnd < 100)
                {
                    win = (long)(amount * Bc.BotConfig.Betroll91Multiplier);
                    str += GetText("br_win", n(win) + CurrencySign, 90);
                    await _cs.AddAsync(ctx.User, "Betroll Gamble",
                        win, false, gamble: true).ConfigureAwait(false);
                }
                else
                {
                    win = (long)(amount * Bc.BotConfig.Betroll100Multiplier);
                    str += GetText("br_win", n(win) + CurrencySign, 99) + " 👑";
                    await _cs.AddAsync(ctx.User, "Betroll Gamble",
                        win, false, gamble: true).ConfigureAwait(false);
                }
            }
            await ctx.Channel.SendConfirmAsync(str).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        public Task BetRoll(ShmartNumber amount)
            => InternallBetroll(amount);

        [WizBotCommand, Usage, Description, Aliases]
        [WizBotOptions(typeof(LbOpts))]
        [Priority(0)]
        public Task Leaderboard(params string[] args)
            => Leaderboard(1, args);

        [WizBotCommand, Usage, Description, Aliases]
        [WizBotOptions(typeof(LbOpts))]
        [Priority(1)]
        public async Task Leaderboard(int page = 1, params string[] args)
        {
            if (--page < 0)
                return;

            var (opts, _) = OptionsParser.ParseFrom(new LbOpts(), args);

            List<DiscordUser> cleanRichest = new List<DiscordUser>();

            // it's pointless to have clean on dm context
            if (Context.Guild is null)
            {
                opts.Clean = false;
            }

            if (opts.Clean)
            {
                var now = DateTime.UtcNow;

                using (var uow = _db.GetDbContext())
                {
                    cleanRichest = uow.DiscordUsers.GetTopRichest(_client.CurrentUser.Id, 10_000);
                }
                
                await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
                await _tracker.EnsureUsersDownloadedAsync(ctx.Guild).ConfigureAwait(false);

                var sg = (SocketGuild)Context.Guild;
                cleanRichest = cleanRichest.Where(x => sg.GetUser(x.UserId) != null)
                    .ToList();
            }
            else
            {
                using (var uow = _db.GetDbContext())
                {
                    cleanRichest = uow.DiscordUsers.GetTopRichest(_client.CurrentUser.Id, 9, page).ToList();
                }
            }

            await Context.SendPaginatedConfirmAsync(page, curPage =>
            {
                var embed = new EmbedBuilder()
                   .WithOkColor()
                   .WithTitle(CurrencySign + " " + GetText("leaderboard"));

                List<DiscordUser> toSend;
                if (!opts.Clean)
                {
                    using (var uow = _db.GetDbContext())
                    {
                        toSend = uow.DiscordUsers.GetTopRichest(_client.CurrentUser.Id, 9, curPage);
                    }
                }

                else
                {
                    toSend = cleanRichest.Skip(curPage * 9).Take(9).ToList();
                }
                if (!toSend.Any())
                {
                    embed.WithDescription(GetText("no_user_on_this_page"));
                    return embed;
                }

                for (var i = 0; i < toSend.Count; i++)
                {
                    var x = toSend[i];
                    var usrStr = x.ToString().TrimTo(20, true);

                    var j = i;
                    embed.AddField(efb => efb.WithName("#" + (9 * curPage + j + 1) + " " + usrStr)
                                             .WithValue(n(x.CurrencyAmount) + " " + CurrencySign)
                                             .WithIsInline(true));
                }

                return embed;
            }, opts.Clean ? cleanRichest.Count() : 9000, 9, opts.Clean);
        }


        public enum RpsPick
        {
            R = 0,
            Rock = 0,
            Rocket = 0,
            P = 1,
            Paper = 1,
            Paperclip = 1,
            S = 2,
            Scissors = 2
        }

        public enum RpsResult
        {
            Win,
            Loss,
            Draw,
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task Rps(RpsPick pick, ShmartNumber amount = default)
        {
            long oldAmount = amount;
            if (!await CheckBetOptional(amount).ConfigureAwait(false) || (amount == 1))
                return;

            string getRpsPick(RpsPick p)
            {
                switch (p)
                {
                    case RpsPick.R:
                        return "🚀";
                    case RpsPick.P:
                        return "📎";
                    default:
                        return "✂️";
                }
            }
            var embed = new EmbedBuilder();

            var wizbotPick = (RpsPick)new WizBotRandom().Next(0, 3);

            if (amount > 0)
            {
                if (!await _cs.RemoveAsync(ctx.User.Id,
                    "Rps-bet", amount, gamble: true).ConfigureAwait(false))
                {
                    await ReplyErrorLocalizedAsync("not_enough", Bc.BotConfig.CurrencySign).ConfigureAwait(false);
                    return;
                }
            }

            string msg;
            if (pick == wizbotPick)
            {
                await _cs.AddAsync(ctx.User.Id,
                    "Rps-draw", amount, gamble: true).ConfigureAwait(false);
                embed.WithOkColor();
                msg = GetText("rps_draw", getRpsPick(pick));
            }
            else if ((pick == RpsPick.Paper && wizbotPick == RpsPick.Rock) ||
                     (pick == RpsPick.Rock && wizbotPick == RpsPick.Scissors) ||
                     (pick == RpsPick.Scissors && wizbotPick == RpsPick.Paper))
            {
                amount = (long)(amount * Bc.BotConfig.BetflipMultiplier);
                await _cs.AddAsync(ctx.User.Id,
                    "Rps-win", amount, gamble: true).ConfigureAwait(false);
                embed.WithOkColor();
                embed.AddField(GetText("won"), n(amount));
                msg = GetText("rps_win", ctx.User.Mention,
                    getRpsPick(pick), getRpsPick(wizbotPick));
            }
            else
            {
                embed.WithErrorColor();
                amount = 0;
                msg = GetText("rps_win", ctx.Client.CurrentUser.Mention, getRpsPick(wizbotPick),
                    getRpsPick(pick));
            }

            embed
                .WithDescription(msg);

            await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }
    }
}
