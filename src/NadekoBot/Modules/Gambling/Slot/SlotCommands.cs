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

// public interface ISlotService
// {
//     ValueTask<OneOf<SlotResult, SlotError>> PullAsync(ulong userId, long amount);
// }

public partial class Gambling
{
    [Group]
    public partial class SlotCommands : GamblingSubmodule<IGamblingService>
    {
        private static decimal totalBet;
        private static decimal totalPaidOut;

        private static readonly HashSet<ulong> _runningUsers = new();

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
                var res = SlotMachine.Pull(0);
                if (dict.ContainsKey(res.Multiplier))
                    dict[res.Multiplier] += 1;
                else
                    dict.Add(res.Multiplier, 1);
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
                        result.Won.ToString(),
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

                    var msg = GetText(strs.better_luck);
                    if (result.Multiplier > 0)
                    {
                        if (Math.Abs(result.Multiplier - 1M) <= decimal.)
                            msg = GetText(strs.slot_single(CurrencySign, 1));
                        else if (Math.Abs(result.Multiplier - 4M) < float.Epsilon)
                            msg = GetText(strs.slot_two(CurrencySign, 4));
                        else if (Math.Abs(result.Multiplier - 10M) <= float.Epsilon)
                            msg = GetText(strs.slot_three(10));
                        else if (Math.Abs(result.Multiplier - 30M) <= float.Epsilon)
                            msg = GetText(strs.slot_jackpot(30));
                    }

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
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    _runningUsers.Remove(ctx.User.Id);
                });
            }
        }
    }
}

public sealed class SlotMachine
{
    public const int MAX_VALUE = 5;

    private static readonly List<Func<int[], int>> _winningCombos = new()
    {
        //three flowers
        arr => arr.All(a => a == MAX_VALUE) ? 30 : 0,
        //three of the same
        arr => arr.All(a => a == arr[0]) ? 10 : 0,
        //two flowers
        arr => arr.Count(a => a == MAX_VALUE) == 2 ? 4 : 0,
        //one flower
        arr => arr.Any(a => a == MAX_VALUE) ? 1 : 0
    };

    public static SlotResult Pull(decimal amount)
    {
        var numbers = new int[3];
        for (var i = 0; i < numbers.Length; i++)
            numbers[i] = new NadekoRandom().Next(0, MAX_VALUE + 1);
        var multi = 0M;
        foreach (var t in _winningCombos)
        {
            multi = t(numbers);
            if (multi != 0)
                break;
        }

        return new()
        {
            Rolls = numbers,
            Multiplier = multi,
            Won = multi * amount,
        };
    }
}