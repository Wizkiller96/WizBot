using Discord;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Searches.Services;
using NadekoBot.Extensions;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        public class CryptoCommands : NadekoSubmodule<CryptoService>
        {
            [NadekoCommand, Aliases]
            public async Task Crypto(string name)
            {
                name = name?.ToUpperInvariant();

                if (string.IsNullOrWhiteSpace(name))
                    return;

                var (crypto, nearest) = await _service.GetCryptoData(name).ConfigureAwait(false);

                if (nearest != null)
                {
                    var embed = _eb.Create()
                            .WithTitle(GetText(strs.crypto_not_found))
                            .WithDescription(GetText(strs.did_you_mean(Format.Bold($"{nearest.Name} ({nearest.Symbol})"))));

                    if (await PromptUserConfirmAsync(embed).ConfigureAwait(false))
                    {
                        crypto = nearest;
                    }
                }

                if (crypto is null)
                {
                    await ReplyErrorLocalizedAsync(strs.crypto_not_found).ConfigureAwait(false);
                    return;
                }

                var sevenDay = decimal.TryParse(crypto.Quote.Usd.Percent_Change_7d, out var sd)
                        ? sd.ToString("F2")
                        : crypto.Quote.Usd.Percent_Change_7d;

                var lastDay = decimal.TryParse(crypto.Quote.Usd.Percent_Change_24h, out var ld)
                        ? ld.ToString("F2")
                        : crypto.Quote.Usd.Percent_Change_24h;

                await ctx.Channel.EmbedAsync(_eb.Create()
                    .WithOkColor()
                    .WithTitle($"{crypto.Name} ({crypto.Symbol})")
                    .WithUrl($"https://coinmarketcap.com/currencies/{crypto.Slug}/")
                    .WithThumbnailUrl($"https://s3.coinmarketcap.com/static/img/coins/128x128/{crypto.Id}.png")
                    .AddField(GetText(strs.market_cap), $"${crypto.Quote.Usd.Market_Cap:n0}", true)
                    .AddField(GetText(strs.price), $"${crypto.Quote.Usd.Price}", true)
                    .AddField(GetText(strs.volume_24h), $"${crypto.Quote.Usd.Volume_24h:n0}", true)
                    .AddField(GetText(strs.change_7d_24h), $"{sevenDay}% / {lastDay}%", true)
                    .WithImageUrl($"https://s3.coinmarketcap.com/generated/sparklines/web/7d/usd/{crypto.Id}.png")).ConfigureAwait(false);
            }
        }
    }
}
