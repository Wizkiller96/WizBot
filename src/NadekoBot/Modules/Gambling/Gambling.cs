using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Gambling.Common;
using NadekoBot.Db.Models;
using NadekoBot.Extensions;
using NadekoBot.Modules.Gambling.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NadekoBot.Db;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling : GamblingModule<GamblingService>
    {
        private readonly DbService _db;
        private readonly ICurrencyService _cs;
        private readonly IDataCache _cache;
        private readonly DiscordSocketClient _client;
        private readonly NumberFormatInfo _enUsCulture;
        private readonly DownloadTracker _tracker;
        private readonly GamblingConfigService _configService;

        public Gambling(DbService db, ICurrencyService currency,
            IDataCache cache, DiscordSocketClient client,
            DownloadTracker tracker, GamblingConfigService configService) : base(configService)
        {
            _db = db;
            _cs = currency;
            _cache = cache;
            _client = client;
            _enUsCulture = new CultureInfo("en-US", false).NumberFormat;
            _enUsCulture.NumberDecimalDigits = 0;
            _enUsCulture.NumberGroupSeparator = " ";
            _tracker = tracker;
            _configService = configService;
        }

        private string n(long cur) => cur.ToString("N", _enUsCulture);

        public string GetCurrency(ulong id)
        {
            using (var uow = _db.GetDbContext())
            {
                return n(uow.DiscordUser.GetUserCurrency(id));
            }
        }

        [NadekoCommand, Aliases]
        public async Task Economy()
        {
            var ec = _service.GetEconomy();
            decimal onePercent = 0;
            if (ec.Cash > 0)
            {
                onePercent = ec.OnePercent / (ec.Cash-ec.Bot); // This stops the top 1% from owning more than 100% of the money
                // [21:03] Bob Page: Kinda remids me of US economy
            }
            var embed = _eb.Create()
                .WithTitle(GetText(strs.economy_state))
                .AddField(GetText(strs.currency_owned), ((BigInteger)(ec.Cash - ec.Bot)) + CurrencySign)
                .AddField(GetText(strs.currency_one_percent), (onePercent * 100).ToString("F2") + "%")
                .AddField(GetText(strs.currency_planted), ((BigInteger)ec.Planted) + CurrencySign)
                .AddField(GetText(strs.owned_waifus_total), ((BigInteger)ec.Waifus) + CurrencySign)
                .AddField(GetText(strs.bot_currency), ec.Bot + CurrencySign)
                .AddField(GetText(strs.total), ((BigInteger)(ec.Cash + ec.Planted + ec.Waifus)).ToString("N", _enUsCulture) + CurrencySign)
                .WithOkColor();
                // ec.Cash already contains ec.Bot as it's the total of all values in the CurrencyAmount column of the DiscordUser table
            await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [NadekoCommand, Aliases]
        public async Task Timely()
        {
            var val = _config.Timely.Amount;
            var period = _config.Timely.Cooldown;
            if (val <= 0 || period <= 0)
            {
                await ReplyErrorLocalizedAsync(strs.timely_none).ConfigureAwait(false);
                return;
            }

            TimeSpan? rem;
            if ((rem = _cache.AddTimelyClaim(ctx.User.Id, period)) != null)
            {
                await ReplyErrorLocalizedAsync(strs.timely_already_claimed(rem?.ToString(@"dd\d\ hh\h\ mm\m\ ss\s"))).ConfigureAwait(false);
                return;
            }

            await _cs.AddAsync(ctx.User.Id, "Timely claim", val).ConfigureAwait(false);

            await ReplyConfirmLocalizedAsync(strs.timely(n(val) + CurrencySign, period));
        }

        [NadekoCommand, Aliases]
        [OwnerOnly]
        public async Task TimelyReset()
        {
            _cache.RemoveAllTimelyClaims();
            await ReplyConfirmLocalizedAsync(strs.timely_reset).ConfigureAwait(false);
        }

        [NadekoCommand, Aliases]
        [OwnerOnly]
        public async Task TimelySet(int amount, int period = 24)
        {
            if (amount < 0 || period < 0)
                return;
            
            _configService.ModifyConfig(gs =>
            {
                gs.Timely.Amount = amount;
                gs.Timely.Cooldown = period;
            });
            
            if (amount == 0)
                await ReplyConfirmLocalizedAsync(strs.timely_set_none).ConfigureAwait(false);
            else
                await ReplyConfirmLocalizedAsync(strs.timely_set(Format.Bold(n(amount) + CurrencySign), Format.Bold(period.ToString()))).ConfigureAwait(false);
        }

        [NadekoCommand, Aliases]
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
            var usr = membersArray[new NadekoRandom().Next(0, membersArray.Length)];
            await SendConfirmAsync("🎟 " + GetText(strs.raffled_user), $"**{usr.Username}#{usr.Discriminator}**", footer: $"ID: {usr.Id}").ConfigureAwait(false);
        }

        [NadekoCommand, Aliases]
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
            var usr = membersArray[new NadekoRandom().Next(0, membersArray.Length)];
            await SendConfirmAsync("🎟 " + GetText(strs.raffled_user), $"**{usr.Username}#{usr.Discriminator}**", footer: $"ID: {usr.Id}").ConfigureAwait(false);
        }

        [NadekoCommand, Aliases]
        [Priority(1)]
        public async Task Cash([Leftover] IUser user = null)
        {
            user = user ?? ctx.User;
            await ConfirmLocalizedAsync(strs.has(Format.Bold(user.ToString()), $"{GetCurrency(user.Id)} {CurrencySign}"));
        }

        [NadekoCommand, Aliases]
        [Priority(2)]
        public Task CurrencyTransactions(int page = 1) =>
            InternalCurrencyTransactions(ctx.User.Id, page);

        [NadekoCommand, Aliases]
        [OwnerOnly]
        [Priority(0)]
        public Task CurrencyTransactions([Leftover] IUser usr) =>
            InternalCurrencyTransactions(usr.Id, 1);

        [NadekoCommand, Aliases]
        [OwnerOnly]
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

            var embed = _eb.Create()
                .WithTitle(GetText(strs.transactions(
                    ((SocketGuild)ctx.Guild)?.GetUser(userId)?.ToString() ?? $"{userId}")))
                .WithOkColor();

            var desc = "";
            foreach (var tr in trs)
            {
                var type = tr.Amount > 0 ? "🔵" : "🔴";
                var date = Format.Code($"〖{tr.DateAdded:HH:mm yyyy-MM-dd}〗");
                desc += $"\\{type} {date} {Format.Bold(n(tr.Amount))}\n\t{tr.Reason?.Trim()}\n";
            }

            embed.WithDescription(desc);
            embed.WithFooter(GetText(strs.page(page + 1)));
            await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [NadekoCommand, Aliases]
        [Priority(0)]
        public async Task Cash(ulong userId)
        {
            await ReplyConfirmLocalizedAsync(strs.has(Format.Code(userId.ToString()), $"{GetCurrency(userId)} {CurrencySign}"));
        }

        [NadekoCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public async Task Give(ShmartNumber amount, IGuildUser receiver, [Leftover] string msg = null)
        {
            if (amount <= 0 || ctx.User.Id == receiver.Id || receiver.IsBot)
                return;
            var success = await _cs.RemoveAsync((IGuildUser)ctx.User, $"Gift to {receiver.Username} ({receiver.Id}).", amount, false).ConfigureAwait(false);
            if (!success)
            {
                await ReplyErrorLocalizedAsync(strs.not_enough(CurrencySign));
                return;
            }
            await _cs.AddAsync(receiver, $"Gift from {ctx.User.Username} ({ctx.User.Id}) - {msg}.", amount, true).ConfigureAwait(false);
            await ReplyConfirmLocalizedAsync(strs.gifted(n(amount) + CurrencySign, Format.Bold(receiver.ToString())));
        }

        [NadekoCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public Task Give(ShmartNumber amount, [Leftover] IGuildUser receiver)
            => Give(amount, receiver, null);

        [NadekoCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        [Priority(0)]
        public Task Award(ShmartNumber amount, IGuildUser usr, [Leftover] string msg) =>
            Award(amount, usr.Id, msg);

        [NadekoCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        [Priority(1)]
        public Task Award(ShmartNumber amount, [Leftover] IGuildUser usr) =>
            Award(amount, usr.Id);

        [NadekoCommand, Aliases]
        [OwnerOnly]
        [Priority(2)]
        public async Task Award(ShmartNumber amount, ulong usrId, [Leftover] string msg = null)
        {
            if (amount <= 0)
                return;

            await _cs.AddAsync(usrId,
                $"Awarded by bot owner. ({ctx.User.Username}/{ctx.User.Id}) {(msg ?? "")}",
                amount,
                gamble: (ctx.Client.CurrentUser.Id != usrId)).ConfigureAwait(false);
            await ReplyConfirmLocalizedAsync(strs.awarded(n(amount) + CurrencySign, $"<@{usrId}>"));
        }

        [NadekoCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        [Priority(2)]
        public async Task Award(ShmartNumber amount, [Leftover] IRole role)
        {
            var users = (await ctx.Guild.GetUsersAsync().ConfigureAwait(false))
                               .Where(u => u.GetRoles().Contains(role))
                               .ToList();

            await _cs.AddBulkAsync(users.Select(x => x.Id),
                users.Select(x => $"Awarded by bot owner to **{role.Name}** role. ({ctx.User.Username}/{ctx.User.Id})"),
                users.Select(x => amount.Value),
                gamble: true)
                .ConfigureAwait(false);

            await ReplyConfirmLocalizedAsync(strs.mass_award(
                n(amount) + CurrencySign,
                Format.Bold(users.Count.ToString()),
                Format.Bold(role.Name)));
        }
        
        [NadekoCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        [Priority(0)]
        public async Task Take(ShmartNumber amount, [Leftover] IRole role)
        {
            var users = (await role.GetMembersAsync()).ToList();

            await _cs.RemoveBulkAsync(users.Select(x => x.Id),
                    users.Select(x => $"Taken by bot owner from **{role.Name}** role. ({ctx.User.Username}/{ctx.User.Id})"),
                    users.Select(x => amount.Value),
                    gamble: true)
                .ConfigureAwait(false);

            await ReplyConfirmLocalizedAsync(strs.mass_take(
                n(amount) + CurrencySign,
                Format.Bold(users.Count.ToString()),
                Format.Bold(role.Name)));
        }

        [NadekoCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        [Priority(1)]
        public async Task Take(ShmartNumber amount, [Leftover] IGuildUser user)
        {
            if (amount <= 0)
                return;

            if (await _cs.RemoveAsync(user, $"Taken by bot owner.({ctx.User.Username}/{ctx.User.Id})", amount,
                gamble: (ctx.Client.CurrentUser.Id != user.Id)).ConfigureAwait(false))
                await ReplyConfirmLocalizedAsync(strs.take(n(amount) + CurrencySign, Format.Bold(user.ToString()))).ConfigureAwait(false);
            else
                await ReplyErrorLocalizedAsync(strs.take_fail(n(amount) + CurrencySign, Format.Bold(user.ToString()), CurrencySign));
        }


        [NadekoCommand, Aliases]
        [OwnerOnly]
        public async Task Take(ShmartNumber amount, [Leftover] ulong usrId)
        {
            if (amount <= 0)
                return;

            if (await _cs.RemoveAsync(usrId, $"Taken by bot owner.({ctx.User.Username}/{ctx.User.Id})", amount,
                gamble: (ctx.Client.CurrentUser.Id != usrId)).ConfigureAwait(false))
                await ReplyConfirmLocalizedAsync(strs.take(amount + CurrencySign, $"<@{usrId}>"));
            else
                await ReplyErrorLocalizedAsync(strs.take_fail(amount + CurrencySign, Format.Code(usrId.ToString()), CurrencySign));
        }

        private IUserMessage rdMsg = null;

        [NadekoCommand, Aliases]
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

        [NadekoCommand, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task RollDuel(ShmartNumber amount, IUser u)
        {
            if (ctx.User.Id == u.Id)
                return;

            if (amount <= 0)
                return;

            var embed = _eb.Create()
                    .WithOkColor()
                    .WithTitle(GetText(strs.roll_duel));

            var description = string.Empty;

            var game = new RollDuelGame(_cs, _client.CurrentUser.Id, ctx.User.Id, u.Id, amount);
            //means challenge is just created
            if (_service.Duels.TryGetValue((ctx.User.Id, u.Id), out var other))
            {
                if (other.Amount != amount)
                {
                    await ReplyErrorLocalizedAsync(strs.roll_duel_already_challenged).ConfigureAwait(false);
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

                await ReplyConfirmLocalizedAsync(strs.roll_duel_challenge(
                    Format.Bold(ctx.User.ToString()),
                    Format.Bold(u.ToString()),
                    Format.Bold(amount + CurrencySign)));
            }

            async Task Game_OnGameTick(RollDuelGame arg)
            {
                var rolls = arg.Rolls.Last();
                description += $@"{Format.Bold(ctx.User.ToString())} rolled **{rolls.Item1}**
{Format.Bold(u.ToString())} rolled **{rolls.Item2}**
--
";
                embed = embed.WithDescription(description);

                if (rdMsg is null)
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
                        description += $"\n**{winner}** Won {n(((long)(rdGame.Amount * 2 * 0.98))) + CurrencySign}";

                        embed = embed.WithDescription(description);
                        
                        await rdMsg.ModifyAsync(x => x.Embed = embed.Build())
                            .ConfigureAwait(false);
                    }
                    else if (reason == RollDuelGame.Reason.Timeout)
                    {
                        await ReplyErrorLocalizedAsync(strs.roll_duel_timeout).ConfigureAwait(false);
                    }
                    else if (reason == RollDuelGame.Reason.NoFunds)
                    {
                        await ReplyErrorLocalizedAsync(strs.roll_duel_no_funds).ConfigureAwait(false);
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
                await ReplyErrorLocalizedAsync(strs.not_enough(CurrencySign));
                return;
            }

            var br = new Betroll(base._config.BetRoll);

            var result = br.Roll();


            var str = Format.Bold(ctx.User.ToString()) + Format.Code(GetText(strs.roll(result.Roll)));
            if (result.Multiplier > 0)
            {
                var win = (long)(amount * result.Multiplier);
                str += GetText(strs.br_win(
                    n(win) + CurrencySign,
                    result.Threshold + (result.Roll == 100 ? " 👑" : "")));
                await _cs.AddAsync(ctx.User, "Betroll Gamble",
                    win, false, gamble: true).ConfigureAwait(false);
            }
            else
            {
                str += GetText(strs.better_luck);
            }
            
            await SendConfirmAsync(str).ConfigureAwait(false);
        }

        [NadekoCommand, Aliases]
        public Task BetRoll(ShmartNumber amount)
            => InternallBetroll(amount);

        [NadekoCommand, Aliases]
        [NadekoOptions(typeof(LbOpts))]
        [Priority(0)]
        public Task Leaderboard(params string[] args)
            => Leaderboard(1, args);

        [NadekoCommand, Aliases]
        [NadekoOptions(typeof(LbOpts))]
        [Priority(1)]
        public async Task Leaderboard(int page = 1, params string[] args)
        {
            if (--page < 0)
                return;

            var (opts, _) = OptionsParser.ParseFrom(new LbOpts(), args);

            List<DiscordUser> cleanRichest = new List<DiscordUser>();
            // it's pointless to have clean on dm context
            if (ctx.Guild is null)
            {
                opts.Clean = false;
            }

            if (opts.Clean)
            {
                var now = DateTime.UtcNow;

                using (var uow = _db.GetDbContext())
                {
                    cleanRichest = uow.DiscordUser.GetTopRichest(_client.CurrentUser.Id, 10_000);
                }
                
                await ctx.Channel.TriggerTypingAsync().ConfigureAwait(false);
                await _tracker.EnsureUsersDownloadedAsync(ctx.Guild).ConfigureAwait(false);

                var sg = (SocketGuild)ctx.Guild;
                cleanRichest = cleanRichest.Where(x => sg.GetUser(x.UserId) != null)
                    .ToList();
            }
            else
            {
                using (var uow = _db.GetDbContext())
                {
                    cleanRichest = uow.DiscordUser.GetTopRichest(_client.CurrentUser.Id, 9, page).ToList();
                }
            }

            await ctx.SendPaginatedConfirmAsync(page, curPage =>
            {
                var embed = _eb.Create()
                   .WithOkColor()
                   .WithTitle(CurrencySign + " " + GetText(strs.leaderboard));

                List<DiscordUser> toSend;
                if (!opts.Clean)
                {
                    using (var uow = _db.GetDbContext())
                    {
                        toSend = uow.DiscordUser.GetTopRichest(_client.CurrentUser.Id, 9, curPage);
                    }
                }
                else
                {
                    toSend = cleanRichest.Skip(curPage * 9).Take(9).ToList();
                }
                 if (!toSend.Any())
                 {
                     embed.WithDescription(GetText(strs.no_user_on_this_page));
                     return embed;
                 }

                 for (var i = 0; i < toSend.Count; i++)
                 {
                     var x = toSend[i];
                     var usrStr = x.ToString().TrimTo(20, true);

                     var j = i;
                     embed.AddField("#" + (9 * curPage + j + 1) + " " + usrStr, n(x.CurrencyAmount) + " " + CurrencySign, true);
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

        [NadekoCommand, Aliases]
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
            var embed = _eb.Create();

            var nadekoPick = (RpsPick)new NadekoRandom().Next(0, 3);

            if (amount > 0)
            {
                if (!await _cs.RemoveAsync(ctx.User.Id,
                    "Rps-bet", amount, gamble: true).ConfigureAwait(false))
                {
                    await ReplyErrorLocalizedAsync(strs.not_enough(CurrencySign));
                    return;
                }
            }

            string msg;
            if (pick == nadekoPick)
            {
                await _cs.AddAsync(ctx.User.Id,
                    "Rps-draw", amount, gamble: true).ConfigureAwait(false);
                embed.WithOkColor();
                msg = GetText(strs.rps_draw(getRpsPick(pick)));
            }
            else if ((pick == RpsPick.Paper && nadekoPick == RpsPick.Rock) ||
                     (pick == RpsPick.Rock && nadekoPick == RpsPick.Scissors) ||
                     (pick == RpsPick.Scissors && nadekoPick == RpsPick.Paper))
            {
                amount = (long)(amount * base._config.BetFlip.Multiplier);
                await _cs.AddAsync(ctx.User.Id,
                    "Rps-win", amount, gamble: true).ConfigureAwait(false);
                embed.WithOkColor();
                embed.AddField(GetText(strs.won), n(amount));
                msg = GetText(strs.rps_win(ctx.User.Mention, getRpsPick(pick), getRpsPick(nadekoPick)));
            }
            else
            {
                embed.WithErrorColor();
                amount = 0;
                msg = GetText(strs.rps_win(ctx.Client.CurrentUser.Mention, getRpsPick(nadekoPick), getRpsPick(pick)));
            }

            embed
                .WithDescription(msg);

            await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }
    }
}
