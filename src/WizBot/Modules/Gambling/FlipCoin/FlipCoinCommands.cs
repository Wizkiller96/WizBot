#nullable disable
using WizBot.Modules.Gambling.Common;
using WizBot.Modules.Gambling.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace WizBot.Modules.Gambling;

public partial class Gambling
{
    [Group]
    public partial class FlipCoinCommands : GamblingSubmodule<GamblingService>
    {
        public enum BetFlipGuess
        {
            H = 1,
            Head = 1,
            Heads = 1,
            T = 2,
            Tail = 2,
            Tails = 2
        }

        private static readonly WizBotRandom _rng = new();
        private readonly IImageCache _images;
        private readonly ICurrencyService _cs;
        private readonly ImagesConfig _ic;

        public FlipCoinCommands(
            IImageCache images,
            ImagesConfig ic,
            ICurrencyService cs,
            GamblingConfigService gss)
            : base(gss)
        {
            _ic = ic;
            _images = images;
            _cs = cs;
        }

        [Cmd]
        public async partial Task Flip(int count = 1)
        {
            if (count is > 10 or < 1)
            {
                await ReplyErrorLocalizedAsync(strs.flip_invalid(10));
                return;
            }

            var headCount = 0;
            var tailCount = 0;
            var imgs = new Image<Rgba32>[count];
            for (var i = 0; i < count; i++)
            {
                var headsArr = await _images.GetHeadsImageAsync();
                var tailsArr = await _images.GetTailsImageAsync();
                if (_rng.Next(0, 10) < 5)
                {
                    imgs[i] = Image.Load(headsArr);
                    headCount++;
                }
                else
                {
                    imgs[i] = Image.Load(tailsArr);
                    tailCount++;
                }
            }

            using var img = imgs.Merge(out var format);
            await using var stream = img.ToStream(format);
            foreach (var i in imgs)
                i.Dispose();

            var msg = count != 1
                ? Format.Bold(ctx.User.ToString())
                  + " "
                  + GetText(strs.flip_results(count, headCount, tailCount))
                : Format.Bold(ctx.User.ToString())
                  + " "
                  + GetText(strs.flipped(headCount > 0
                      ? Format.Bold(GetText(strs.heads))
                      : Format.Bold(GetText(strs.tails))));

            await ctx.Channel.SendFileAsync(stream, $"{count} coins.{format.FileExtensions.First()}", msg);
        }

        [Cmd]
        public async partial Task Betflip(ShmartNumber amount, BetFlipGuess guess)
        {
            if (!await CheckBetMandatory(amount) || amount == 1)
                return;

            var removed = await _cs.RemoveAsync(ctx.User, amount, new("betflip", "bet"));
            if (!removed)
            {
                await ReplyErrorLocalizedAsync(strs.not_enough(CurrencySign));
                return;
            }

            BetFlipGuess result;
            Uri imageToSend;
            var coins = _ic.Data.Coins;
            if (_rng.Next(0, 1000) <= 499)
            {
                imageToSend = coins.Heads[_rng.Next(0, coins.Heads.Length)];
                result = BetFlipGuess.Heads;
            }
            else
            {
                imageToSend = coins.Tails[_rng.Next(0, coins.Tails.Length)];
                result = BetFlipGuess.Tails;
            }

            string str;
            if (guess == result)
            {
                var toWin = (long)(amount * Config.BetFlip.Multiplier);
                str = Format.Bold(ctx.User.ToString()) + " " + GetText(strs.flip_guess(N(toWin)));
                await _cs.AddAsync(ctx.User, toWin, new("betflip", "win"));
            }
            else
                str = Format.Bold(ctx.User.ToString()) + " " + GetText(strs.better_luck);

            await ctx.Channel.EmbedAsync(_eb.Create()
                                            .WithDescription(str)
                                            .WithOkColor()
                                            .WithImageUrl(imageToSend.ToString()));
        }
    }
}