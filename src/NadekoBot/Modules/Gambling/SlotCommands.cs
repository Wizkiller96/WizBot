using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Db.Models;
using NadekoBot.Modules.Gambling.Services;
using NadekoBot.Modules.Gambling.Common;
using NadekoBot.Services;
using SixLabors.Fonts;
using Image = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using Color = SixLabors.ImageSharp.Color;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class SlotCommands : GamblingSubmodule<GamblingService>
        {
            private static long _totalBet;
            private static long _totalPaidOut;

            private static readonly HashSet<ulong> _runningUsers = new HashSet<ulong>();

            //here is a payout chart
            //https://lh6.googleusercontent.com/-i1hjAJy_kN4/UswKxmhrbPI/AAAAAAAAB1U/82wq_4ZZc-Y/DE6B0895-6FC1-48BE-AC4F-14D1B91AB75B.jpg
            //thanks to judge for helping me with this

            private readonly IImageCache _images;
            private FontProvider _fonts;
            private readonly DbService _db;

            public SlotCommands(IDataCache data,
                FontProvider fonts, DbService db,
                GamblingConfigService gamb) : base(gamb)
            {
                _images = data.LocalImages;
                _fonts = fonts;
                _db = db;
            }

            public sealed class SlotMachine
            {
                public const int MaxValue = 5;

                static readonly List<Func<int[], int>> _winningCombos = new List<Func<int[], int>>()
                {
                    //three flowers
                    (arr) => arr.All(a=>a==MaxValue) ? 30 : 0,
                    //three of the same
                    (arr) => !arr.Any(a => a != arr[0]) ? 10 : 0,
                    //two flowers
                    (arr) => arr.Count(a => a == MaxValue) == 2 ? 4 : 0,
                    //one flower
                    (arr) => arr.Any(a => a == MaxValue) ? 1 : 0,
                };

                public static SlotResult Pull()
                {
                    var numbers = new int[3];
                    for (var i = 0; i < numbers.Length; i++)
                    {
                        numbers[i] = new NadekoRandom().Next(0, MaxValue + 1);
                    }
                    var multi = 0;
                    foreach (var t in _winningCombos)
                    {
                        multi = t(numbers);
                        if (multi != 0)
                            break;
                    }

                    return new SlotResult(numbers, multi);
                }

                public struct SlotResult
                {
                    public int[] Numbers { get; }
                    public int Multiplier { get; }
                    public SlotResult(int[] nums, int multi)
                    {
                        Numbers = nums;
                        Multiplier = multi;
                    }
                }
            }

            [NadekoCommand, Aliases]
            [OwnerOnly]
            public async Task SlotStats()
            {
                //i remembered to not be a moron
                var paid = _totalPaidOut;
                var bet = _totalBet;

                if (bet <= 0)
                    bet = 1;

                var embed = _eb.Create()
                    .WithOkColor()
                    .WithTitle("Slot Stats")
                    .AddField("Total Bet", bet.ToString(), true)
                    .AddField("Paid Out", paid.ToString(), true)
                    .WithFooter($"Payout Rate: {paid * 1.0 / bet * 100:f4}%");

                await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
            [OwnerOnly]
            public async Task SlotTest(int tests = 1000)
            {
                if (tests <= 0)
                    return;
                //multi vs how many times it occured
                var dict = new Dictionary<int, int>();
                for (int i = 0; i < tests; i++)
                {
                    var res = SlotMachine.Pull();
                    if (dict.ContainsKey(res.Multiplier))
                        dict[res.Multiplier] += 1;
                    else
                        dict.Add(res.Multiplier, 1);
                }

                var sb = new StringBuilder();
                const int bet = 1;
                int payout = 0;
                foreach (var key in dict.Keys.OrderByDescending(x => x))
                {
                    sb.AppendLine($"x{key} occured {dict[key]} times. {dict[key] * 1.0f / tests * 100}%");
                    payout += key * dict[key];
                }
                await SendConfirmAsync("Slot Test Results", sb.ToString(),
                    footer: $"Total Bet: {tests * bet} | Payout: {payout * bet} | {payout * 1.0f / tests * 100}%").ConfigureAwait(false);
            }

            [NadekoCommand, Aliases]
            public async Task Slot(ShmartNumber amount)
            {
               if (!_runningUsers.Add(ctx.User.Id))
                    return;
               
               try
               {
                   if (!await CheckBetMandatory(amount).ConfigureAwait(false))
                       return;
                    
                   await ctx.Channel.TriggerTypingAsync().ConfigureAwait(false);

                   var result = await _service.SlotAsync(ctx.User.Id, amount);

                   if (result.Error != GamblingError.None)
                   {
                       if (result.Error == GamblingError.NotEnough)
                       {
                           await ReplyErrorLocalizedAsync(strs.not_enough(CurrencySign));
                       }

                       return;
                   }

                   Interlocked.Add(ref _totalBet, amount);
                   Interlocked.Add(ref _totalPaidOut, result.Won);

                   long ownedAmount;
                   using (var uow = _db.GetDbContext())
                   {
                       ownedAmount = uow.Set<DiscordUser>()
                           .FirstOrDefault(x => x.UserId == ctx.User.Id)
                           ?.CurrencyAmount ?? 0;
                   }

                   using (var bgImage = Image.Load<Rgba32>(_images.SlotBackground, out var format))
                   {
                       var numbers = new int[3];
                       result.Rolls.CopyTo(numbers, 0);

                       Color fontColor = _config.Slots.CurrencyFontColor;
                       
                       bgImage.Mutate(x => x.DrawText(new TextGraphicsOptions
                           {
                               TextOptions = new TextOptions()
                               {
                                   HorizontalAlignment = HorizontalAlignment.Center,
                                   VerticalAlignment = VerticalAlignment.Center,
                                   WrapTextWidth = 140,
                               }
                           }, result.Won.ToString(), _fonts.DottyFont.CreateFont(65), fontColor,
                           new PointF(227, 92)));

                       var bottomFont = _fonts.DottyFont.CreateFont(50);
                       
                       bgImage.Mutate(x => x.DrawText(new TextGraphicsOptions
                           {
                               TextOptions = new TextOptions()
                               {
                                   HorizontalAlignment = HorizontalAlignment.Center,
                                   VerticalAlignment = VerticalAlignment.Center,
                                   WrapTextWidth = 135,
                               }
                           }, amount.ToString(), bottomFont, fontColor,
                           new PointF(129, 472)));

                       bgImage.Mutate(x => x.DrawText(new TextGraphicsOptions
                           {
                               TextOptions = new TextOptions()
                               {
                                   HorizontalAlignment = HorizontalAlignment.Center,
                                   VerticalAlignment = VerticalAlignment.Center,
                                   WrapTextWidth = 135,
                               }
                           }, ownedAmount.ToString(), bottomFont, fontColor,
                           new PointF(325, 472)));
                       //sw.PrintLap("drew red text");

                       for (var i = 0; i < 3; i++)
                       {
                           using (var img = Image.Load(_images.SlotEmojis[numbers[i]]))
                           {
                               bgImage.Mutate(x => x.DrawImage(img, new Point(148 + 105 * i, 217), 1f));
                           }
                       }

                       var msg = GetText(strs.better_luck);
                       if (result.Multiplier > 0)
                       {
                           if (result.Multiplier == 1f)
                               msg = GetText(strs.slot_single(CurrencySign, 1));
                           else if (result.Multiplier == 4f)
                               msg = GetText(strs.slot_two(CurrencySign, 4));
                           else if (result.Multiplier == 10f)
                               msg = GetText(strs.slot_three(10));
                           else if (result.Multiplier == 30f)
                               msg = GetText(strs.slot_jackpot(30));
                       }

                       using (var imgStream = bgImage.ToStream())
                       {
                           await ctx.Channel.SendFileAsync(imgStream,
                               filename: "result.png",
                               text: Format.Bold(ctx.User.ToString()) + " " + msg).ConfigureAwait(false);
                       }
                   }
               }
               finally
               {
                   var _ = Task.Run(async () =>
                   {
                       await Task.Delay(1000).ConfigureAwait(false);
                       _runningUsers.Remove(ctx.User.Id);
                   });
               }
            }
        }
    }
}
