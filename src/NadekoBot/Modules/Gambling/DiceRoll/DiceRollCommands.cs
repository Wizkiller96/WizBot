#nullable disable
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text.RegularExpressions;
using Image = SixLabors.ImageSharp.Image;

namespace NadekoBot.Modules.Gambling;

public partial class Gambling
{
    [Group]
    public partial class DiceRollCommands : NadekoModule
    {
        private static readonly Regex _dndRegex = new(@"^(?<n1>\d+)d(?<n2>\d+)(?:\+(?<add>\d+))?(?:\-(?<sub>\d+))?$",
            RegexOptions.Compiled);

        private static readonly Regex _fudgeRegex = new(@"^(?<n1>\d+)d(?:F|f)$", RegexOptions.Compiled);

        private static readonly char[] _fateRolls = { '-', ' ', '+' };
        private readonly IImageCache _images;

        public DiceRollCommands(IDataCache data)
            => _images = data.LocalImages;

        [Cmd]
        public async partial Task Roll()
        {
            var rng = new NadekoRandom();
            var gen = rng.Next(1, 101);

            var num1 = gen / 10;
            var num2 = gen % 10;

            using var img1 = GetDice(num1);
            using var img2 = GetDice(num2);
            using var img = new[] { img1, img2 }.Merge(out var format);
            await using var ms = img.ToStream(format);
            await ctx.Channel.SendFileAsync(ms,
                $"dice.{format.FileExtensions.First()}",
                Format.Bold(ctx.User.ToString()) + " " + GetText(strs.dice_rolled(Format.Code(gen.ToString()))));
        }

        [Cmd]
        [Priority(1)]
        public async partial Task Roll(int num)
            => await InternalRoll(num, true);


        [Cmd]
        [Priority(1)]
        public async partial Task Rolluo(int num = 1)
            => await InternalRoll(num, false);

        [Cmd]
        [Priority(0)]
        public async partial Task Roll(string arg)
            => await InternallDndRoll(arg, true);

        [Cmd]
        [Priority(0)]
        public async partial Task Rolluo(string arg)
            => await InternallDndRoll(arg, false);

        private async Task InternalRoll(int num, bool ordered)
        {
            if (num is < 1 or > 30)
            {
                await ReplyErrorLocalizedAsync(strs.dice_invalid_number(1, 30));
                return;
            }

            var rng = new NadekoRandom();

            var dice = new List<Image<Rgba32>>(num);
            var values = new List<int>(num);
            for (var i = 0; i < num; i++)
            {
                var randomNumber = rng.Next(1, 7);
                var toInsert = dice.Count;
                if (ordered)
                {
                    if (randomNumber == 6 || dice.Count == 0)
                        toInsert = 0;
                    else if (randomNumber != 1)
                    {
                        for (var j = 0; j < dice.Count; j++)
                        {
                            if (values[j] < randomNumber)
                            {
                                toInsert = j;
                                break;
                            }
                        }
                    }
                }
                else
                    toInsert = dice.Count;

                dice.Insert(toInsert, GetDice(randomNumber));
                values.Insert(toInsert, randomNumber);
            }

            using var bitmap = dice.Merge(out var format);
            await using var ms = bitmap.ToStream(format);
            foreach (var d in dice)
                d.Dispose();

            await ctx.Channel.SendFileAsync(ms,
                $"dice.{format.FileExtensions.First()}",
                Format.Bold(ctx.User.ToString())
                + " "
                + GetText(strs.dice_rolled_num(Format.Bold(values.Count.ToString())))
                + " "
                + GetText(strs.total_average(Format.Bold(values.Sum().ToString()),
                    Format.Bold((values.Sum() / (1.0f * values.Count)).ToString("N2")))));
        }

        private async Task InternallDndRoll(string arg, bool ordered)
        {
            Match match;
            if ((match = _fudgeRegex.Match(arg)).Length != 0
                && int.TryParse(match.Groups["n1"].ToString(), out var n1)
                && n1 is > 0 and < 500)
            {
                var rng = new NadekoRandom();

                var rolls = new List<char>();

                for (var i = 0; i < n1; i++)
                    rolls.Add(_fateRolls[rng.Next(0, _fateRolls.Length)]);
                var embed = _eb.Create()
                               .WithOkColor()
                               .WithDescription(ctx.User.Mention
                                                + " "
                                                + GetText(strs.dice_rolled_num(Format.Bold(n1.ToString()))))
                               .AddField(Format.Bold("Result"),
                                   string.Join(" ", rolls.Select(c => Format.Code($"[{c}]"))));

                await ctx.Channel.EmbedAsync(embed);
            }
            else if ((match = _dndRegex.Match(arg)).Length != 0)
            {
                var rng = new NadekoRandom();
                if (int.TryParse(match.Groups["n1"].ToString(), out n1)
                    && int.TryParse(match.Groups["n2"].ToString(), out var n2)
                    && n1 <= 50
                    && n2 <= 100000
                    && n1 > 0
                    && n2 > 0)
                {
                    if (!int.TryParse(match.Groups["add"].Value, out var add))
                        add = 0;
                    if (!int.TryParse(match.Groups["sub"].Value, out var sub))
                        sub = 0;

                    var arr = new int[n1];
                    for (var i = 0; i < n1; i++)
                        arr[i] = rng.Next(1, n2 + 1);

                    var sum = arr.Sum();
                    var embed = _eb.Create()
                                   .WithOkColor()
                                   .WithDescription(ctx.User.Mention
                                                    + " "
                                                    + GetText(strs.dice_rolled_num(n1 + $"`1 - {n2}`")))
                                   .AddField(Format.Bold("Rolls"),
                                       string.Join(" ",
                                           (ordered ? arr.OrderBy(x => x).AsEnumerable() : arr).Select(x
                                               => Format.Code(x.ToString()))))
                                   .AddField(Format.Bold("Sum"),
                                       sum + " + " + add + " - " + sub + " = " + (sum + add - sub));
                    await ctx.Channel.EmbedAsync(embed);
                }
            }
        }

        [Cmd]
        public async partial Task NRoll([Leftover] string range)
        {
            int rolled;
            if (range.Contains("-"))
            {
                var arr = range.Split('-').Take(2).Select(int.Parse).ToArray();
                if (arr[0] > arr[1])
                {
                    await ReplyErrorLocalizedAsync(strs.second_larger_than_first);
                    return;
                }

                rolled = new NadekoRandom().Next(arr[0], arr[1] + 1);
            }
            else
                rolled = new NadekoRandom().Next(0, int.Parse(range) + 1);

            await ReplyConfirmLocalizedAsync(strs.dice_rolled(Format.Bold(rolled.ToString())));
        }

        private Image<Rgba32> GetDice(int num)
        {
            if (num is < 0 or > 10)
                throw new ArgumentOutOfRangeException(nameof(num));

            if (num == 10)
            {
                var images = _images.Dice;
                using var imgOne = Image.Load(images[1]);
                using var imgZero = Image.Load(images[0]);
                return new[] { imgOne, imgZero }.Merge();
            }

            return Image.Load(_images.Dice[num]);
        }
    }
}