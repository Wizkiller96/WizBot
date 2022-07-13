#nullable disable
using NadekoBot.Db.Models;
using NadekoBot.Modules.Gambling.Common;
using NadekoBot.Modules.Gambling.Services;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text;
using Nadeko.Econ.Gambling;
using Color = SixLabors.ImageSharp.Color;
using Image = SixLabors.ImageSharp.Image;

namespace NadekoBot.Modules.Gambling;

public enum GamblingError
{
    InsufficientFunds,
}

public partial class Gambling
{
    [Group]
    public partial class SlotCommands : GamblingSubmodule<IGamblingService>
    {
        private static decimal totalBet;
        private static decimal totalPaidOut;

        private static readonly ConcurrentHashSet<ulong> _runningUsers = new();

        //here is a payout chart
        //https://lh6.googleusercontent.com/-i1hjAJy_kN4/UswKxmhrbPI/AAAAAAAAB1U/82wq_4ZZc-Y/DE6B0895-6FC1-48BE-AC4F-14D1B91AB75B.jpg
        //thanks to judge for helping me with this

        private readonly IImageCache _images;
        private readonly FontProvider _fonts;
        private readonly DbService _db;
        private object _slotStatsLock = new();

        public SlotCommands(
            ImageCache images,
            FontProvider fonts,
            DbService db,
            GamblingConfigService gamb)
            : base(gamb)
        {
            _images = images;
            _fonts = fonts;
            _db = db;
        }

        public Task Test()
            => Task.CompletedTask;

        [Cmd]
        [OwnerOnly]
        public async partial Task SlotStats()
        {
            //i remembered to not be a moron
            var paid = totalPaidOut;
            var bet = totalBet;

            if (bet <= 0)
                bet = 1;

            var embed = _eb.Create()
                           .WithOkColor()
                           .WithTitle("Slot Stats")
                           .AddField("Total Bet", N(bet), true)
                           .AddField("Paid Out", N(paid), true)
                           .WithFooter($"Payout Rate: {paid * 1.0M / bet * 100:f4}%");

            await ctx.Channel.EmbedAsync(embed);
        }

        [Cmd]
        [OwnerOnly]
        public async partial Task SlotTest(int tests = 1000)
        {
            if (tests <= 0)
                return;
            //multi vs how many times it occured
            var dict = new Dictionary<decimal, int>();
            for (var i = 0; i < tests; i++)
            {
                var res = await _service.SlotAsync(ctx.User.Id, 0);
                var multi = res.AsT0.Multiplier;
                if (dict.ContainsKey(multi))
                    dict[multi] += 1;
                else
                    dict.Add(multi, 1);
            }

            var sb = new StringBuilder();
            decimal payout = 0;
            foreach (var key in dict.Keys.OrderByDescending(x => x))
            {
                sb.AppendLine($"x{key} occured {dict[key]} times. {dict[key] * 1.0f / tests * 100}%");
                payout += key * dict[key];
            }

            await SendConfirmAsync("Slot Test Results",
                sb.ToString(),
                footer: $"Total Bet: {tests} | Payout: {payout:F0} | {payout * 1.0M / tests * 100}%");
        }

        [Cmd]
        public async partial Task Slot(ShmartNumber amount)
        {
            if (!_runningUsers.Add(ctx.User.Id))
                return;

            try
            {
                if (!await CheckBetMandatory(amount))
                    return;

                await ctx.Channel.TriggerTypingAsync();

                var maybeResult = await _service.SlotAsync(ctx.User.Id, amount);

                if (!maybeResult.TryPickT0(out var result, out var error))
                {
                    if (error == GamblingError.InsufficientFunds)
                        await ReplyErrorLocalizedAsync(strs.not_enough(CurrencySign));

                    return;
                }

                lock (_slotStatsLock)
                {
                    totalBet += amount;
                    totalPaidOut += result.Won;
                }

                long ownedAmount;
                await using (var uow = _db.GetDbContext())
                {
                    ownedAmount = uow.Set<DiscordUser>().FirstOrDefault(x => x.UserId == ctx.User.Id)?.CurrencyAmount
                                  ?? 0;
                }

                var slotBg = await _images.GetSlotBgAsync();
                using (var bgImage = Image.Load<Rgba32>(slotBg, out _))
                {
                    var numbers = new int[3];
                    result.Rolls.CopyTo(numbers, 0);

                    Color fontColor = Config.Slots.CurrencyFontColor;

                    bgImage.Mutate(x => x.DrawText(new()
                        {
                            TextOptions = new()
                            {
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                WrapTextWidth = 140
                            }
                        },
                        ((long)result.Won).ToString(),
                        _fonts.DottyFont.CreateFont(65),
                        fontColor,
                        new(227, 92)));

                    var bottomFont = _fonts.DottyFont.CreateFont(50);

                    bgImage.Mutate(x => x.DrawText(new()
                        {
                            TextOptions = new()
                            {
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                WrapTextWidth = 135
                            }
                        },
                        amount.ToString(),
                        bottomFont,
                        fontColor,
                        new(129, 472)));

                    bgImage.Mutate(x => x.DrawText(new()
                        {
                            TextOptions = new()
                            {
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                WrapTextWidth = 135
                            }
                        },
                        ownedAmount.ToString(),
                        bottomFont,
                        fontColor,
                        new(325, 472)));
                    //sw.PrintLap("drew red text");

                    for (var i = 0; i < 3; i++)
                    {
                        using var img = Image.Load(await _images.GetSlotEmojiAsync(numbers[i]));
                        bgImage.Mutate(x => x.DrawImage(img, new Point(148 + (105 * i), 217), 1f));
                    }

                    var multi = result.Multiplier.ToString("0.##");
                    var msg = result.WinType switch
                    {
                        SlotWinType.SingleJoker => GetText(strs.slot_single(CurrencySign, multi)),
                        SlotWinType.DoubleJoker => GetText(strs.slot_two(CurrencySign, multi)),
                        SlotWinType.TrippleNormal => GetText(strs.slot_three(multi)),
                        SlotWinType.TrippleJoker => GetText(strs.slot_jackpot(multi)),
                        _ => GetText(strs.better_luck),
                    };

                    await using (var imgStream = await bgImage.ToStreamAsync())
                    {
                        await ctx.Channel.SendFileAsync(imgStream,
                            "result.png",
                            Format.Bold(ctx.User.ToString()) + " " + msg);
                    }
                }
            }
            finally
            {
                await Task.Delay(1000);
                _runningUsers.TryRemove(ctx.User.Id);
            }
        }
    }
}