#nullable disable
using NadekoBot.Modules.Searches.Services;

namespace NadekoBot.Modules.Searches;

public partial class Searches
{
    public partial class CryptoCommands : NadekoSubmodule<CryptoService>
    {
        [Cmd]
        public async partial Task Crypto(string name)
        {
            name = name?.ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(name))
                return;

            var (crypto, nearest) = await _service.GetCryptoData(name);

            if (nearest is not null)
            {
                var embed = _eb.Create()
                               .WithTitle(GetText(strs.crypto_not_found))
                               .WithDescription(
                                   GetText(strs.did_you_mean(Format.Bold($"{nearest.Name} ({nearest.Symbol})"))));

                if (await PromptUserConfirmAsync(embed))
                    crypto = nearest;
            }

            if (crypto is null)
            {
                await ReplyErrorLocalizedAsync(strs.crypto_not_found);
                return;
            }

            var usd = crypto.Quote["USD"];

            var sevenDay = usd.PercentChange7d.ToString("F2", Culture);
            var lastDay = usd.PercentChange24h.ToString("F2", Culture);
            var price = usd.Price < 0.01
                ? usd.Price.ToString(Culture)
                : usd.Price.ToString("F2", Culture);

            var volume = usd.Volume24h.ToString("n0", Culture);
            var marketCap = usd.MarketCap.ToString("n0", Culture);

            await ctx.Channel.EmbedAsync(_eb.Create()
                                            .WithOkColor()
                                            .WithAuthor($"#{crypto.CmcRank}")
                                            .WithTitle($"{crypto.Name} ({crypto.Symbol})")
                                            .WithUrl($"https://coinmarketcap.com/currencies/{crypto.Slug}/")
                                            .WithThumbnailUrl(
                                                $"https://s3.coinmarketcap.com/static/img/coins/128x128/{crypto.Id}.png")
                                            .AddField(GetText(strs.market_cap),
                                                $"${marketCap}",
                                                true)
                                            .AddField(GetText(strs.price), $"${price}", true)
                                            .AddField(GetText(strs.volume_24h), $"${volume}", true)
                                            .AddField(GetText(strs.change_7d_24h), $"{sevenDay}% / {lastDay}%", true)
                                            .WithImageUrl(
                                                $"https://s3.coinmarketcap.com/generated/sparklines/web/7d/usd/{crypto.Id}.png"));
        }
    }
}