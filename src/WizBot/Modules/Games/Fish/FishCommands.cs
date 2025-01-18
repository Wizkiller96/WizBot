using System.Text;
using Format = Discord.Format;

namespace WizBot.Modules.Games;

public partial class Games
{
    public class FishCommands(
        FishService fs,
        FishConfigService fcs,
        IBotCache cache,
        CaptchaService service) : WizBotModule
    {
        private TypedKey<bool> FishingWhitelistKey(ulong userId)
            => new($"fishingwhitelist:{userId}");

        [Cmd]
        public async Task Fish()
        {
            var cRes = await cache.GetAsync(FishingWhitelistKey(ctx.User.Id));
            if (cRes.TryPickT1(out _, out _))
            {
                var password = await GetUserCaptcha(ctx.User.Id);
                var img = service.GetPasswordImage(password);
                using var stream = await img.ToStreamAsync();
                var captcha = await Response()
                                    .File(stream, "timely.png")
                                    .SendAsync();

                try
                {
                    var userInput = await GetUserInputAsync(ctx.User.Id, ctx.Channel.Id);
                    if (userInput?.ToLowerInvariant() != password?.ToLowerInvariant())
                    {
                        return;
                    }

                    // whitelist the user for 30 minutes
                    await cache.AddAsync(FishingWhitelistKey(ctx.User.Id), true, TimeSpan.FromMinutes(30));
                    // reset the password
                    await ClearUserCaptcha(ctx.User.Id);
                }
                finally
                {
                    _ = captcha.DeleteAsync();
                }
            }


            var fishResult = await fs.FishAsync(ctx.User.Id, ctx.Channel.Id);
            if (fishResult.TryPickT1(out _, out var fishTask))
            {
                return;
            }

            var currentWeather = fs.GetCurrentWeather();
            var currentTod = fs.GetTime();
            var spot = fs.GetSpot(ctx.Channel.Id);

            var msg = await Response()
                            .Embed(CreateEmbed()
                                   .WithPendingColor()
                                   .WithAuthor(ctx.User)
                                   .WithDescription(GetText(strs.fish_waiting))
                                   .AddField(GetText(strs.fish_spot), GetSpotEmoji(spot) + " " + spot.ToString(), true)
                                   .AddField(GetText(strs.fish_weather),
                                       GetWeatherEmoji(currentWeather) + " " + currentWeather,
                                       true)
                                   .AddField(GetText(strs.fish_tod), GetTodEmoji(currentTod) + " " + currentTod, true))
                            .SendAsync();

            var res = await fishTask;
            if (res is null)
            {
                await Response().Error(strs.fish_nothing).SendAsync();
                return;
            }


            await Response()
                  .Embed(CreateEmbed()
                         .WithOkColor()
                         .WithAuthor(ctx.User)
                         .WithDescription(GetText(strs.fish_caught(Format.Bold(res.Fish.Name))))
                         .AddField(GetText(strs.fish_quality), GetStarText(res.Stars, res.Fish.Stars), true)
                         .AddField(GetText(strs.desc), res.Fish.Fluff, true)
                         .WithThumbnailUrl(res.Fish.Image))
                  .SendAsync();

            await msg.DeleteAsync();
        }

        [Cmd]
        public async Task FishSpot()
        {
            var ws = fs.GetWeatherForPeriods(7);
            var spot = fs.GetSpot(ctx.Channel.Id);
            var time = fs.GetTime();

            await Response()
                  .Embed(CreateEmbed()
                         .WithOkColor()
                         .WithDescription(GetText(strs.fish_weather_duration(fs.GetWeatherPeriodDuration())))
                         .AddField(GetText(strs.fish_spot), GetSpotEmoji(spot) + " " + spot, true)
                         .AddField(GetText(strs.fish_tod), GetTodEmoji(time) + " " + time, true)
                         .AddField(GetText(strs.fish_weather_forecast),
                             ws.Select(x => GetWeatherEmoji(x)).Join(""),
                             true))
                  .SendAsync();
        }

        [Cmd]
        public async Task Fishlist(int page = 1)
        {
            if (--page < 0)
                return;

            var fishes = await fs.GetAllFish();

            Log.Information(fishes.Count.ToString());
            var catches = await fs.GetUserCatches(ctx.User.Id);

            var catchDict = catches.ToDictionary(x => x.FishId, x => x);

            await Response()
                  .Paginated()
                  .Items(fishes)
                  .PageSize(9)
                  .CurrentPage(page)
                  .Page((fs, i) =>
                  {
                      var eb = CreateEmbed()
                          .WithOkColor();

                      foreach (var f in fs)
                      {
                          if (catchDict.TryGetValue(f.Id, out var c))
                          {
                              eb.AddField(f.Name,
                                  GetFishEmoji(f, c.Count)
                                  + " "
                                  + GetSpotEmoji(f.Spot)
                                  + GetTodEmoji(f.Time)
                                  + GetWeatherEmoji(f.Weather)
                                  + "\n"
                                  + GetStarText(c.MaxStars, f.Stars)
                                  + "\n"
                                  + Format.Italics(f.Fluff),
                                  true);
                          }
                          else
                          {
                              eb.AddField("?", GetFishEmoji(null, 0) + "\n" + GetStarText(0, f.Stars), true);
                          }
                      }

                      return eb;
                  })
                  .SendAsync();
        }

        private string GetFishEmoji(FishData? fish, int count)
        {
            if (fish is null)
                return "";

            return fish.Emoji + " x" + count;
        }

        private string GetSpotEmoji(FishingSpot? spot)
        {
            if (spot is not FishingSpot fs)
                return string.Empty;

            var conf = fcs.Data;

            return conf.SpotEmojis[(int)fs];
        }

        private string GetTodEmoji(FishingTime? fishTod)
        {
            return fishTod switch
            {
                FishingTime.Night => "🌑",
                FishingTime.Dawn => "🌅",
                FishingTime.Dusk => "🌆",
                FishingTime.Day => "☀️",
                _ => ""
            };
        }

        private string GetWeatherEmoji(FishingWeather? w)
            => w switch
            {
                FishingWeather.Rain => "🌧️",
                FishingWeather.Snow => "❄️",
                FishingWeather.Storm => "⛈️",
                FishingWeather.Clear => "☀️",
                _ => ""
            };

        private string GetStarText(int resStars, int fishStars)
        {
            if (resStars == fishStars)
            {
                return MultiplyStars(fcs.Data.StarEmojis[^1], fishStars);
            }

            var c = fcs.Data;
            var starsp1 = MultiplyStars(c.StarEmojis[resStars], resStars);
            var starsp2 = MultiplyStars(c.StarEmojis[0], fishStars - resStars);

            return starsp1 + starsp2;
        }

        private string MultiplyStars(string starEmoji, int count)
        {
            var sb = new StringBuilder();

            for (var i = 0; i < count; i++)
            {
                sb.Append(starEmoji);
            }

            return sb.ToString();
        }

        private static TypedKey<string> CaptchaPasswordKey(ulong userId)
            => new($"timely_password:{userId}");

        private async Task<string> GetUserCaptcha(ulong userId)
        {
            var pw = await cache.GetOrAddAsync(CaptchaPasswordKey(userId),
                () =>
                {
                    var password = service.GeneratePassword();
                    return Task.FromResult(password)!;
                });

            return pw!;
        }

        private ValueTask<bool> ClearUserCaptcha(ulong userId)
            => cache.RemoveAsync(CaptchaPasswordKey(userId));
    }
}

//
// public sealed class UserFishStats
// {
//     [Key]
//     public int Id { get; set; }
//
//     public ulong UserId { get; set; }
//
//     public ulong CommonCatches { get; set; }
//     public ulong RareCatches { get; set; }
//     public ulong VeryRareCatches { get; set; }
//     public ulong EpicCatches { get; set; }
//
//     public ulong CommonMaxCatches { get; set; }
//     public ulong RareMaxCatches { get; set; }
//     public ulong VeryRareMaxCatches { get; set; }
//     public ulong EpicMaxCatches { get; set; }
//
//     public int TotalStars { get; set; }
// }

public enum FishingSpot
{
    Ocean,
    River,
    Lake,
    Swamp,
    Reef
}

public enum FishingTime
{
    Night,
    Dawn,
    Day,
    Dusk
}

public enum FishingWeather
{
    Clear,
    Rain,
    Storm,
    Snow
}