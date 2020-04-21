﻿using Discord;
using Discord.Commands;
using WizBot.Common.Attributes;
using WizBot.Core.Modules.Searches.Services;
using WizBot.Extensions;
using System.Threading.Tasks;

namespace WizBot.Modules.Searches
{
    public partial class Searches
    {
        public class CryptoCommands : WizBotSubmodule<CryptoService>
        {
            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Crypto(string name)
            {
                name = name?.ToUpperInvariant();

                if (string.IsNullOrWhiteSpace(name))
                    return;

                var (crypto, nearest) = await _service.GetCryptoData(name).ConfigureAwait(false);

                if (nearest != null)
                {
                    var embed = new EmbedBuilder()
                            .WithTitle(GetText("crypto_not_found"))
                            .WithDescription(GetText("did_you_mean", Format.Bold($"{nearest.Name} ({nearest.Symbol})")));

                    if (await PromptUserConfirmAsync(embed).ConfigureAwait(false))
                    {
                        crypto = nearest;
                    }
                }

                if (crypto == null)
                {
                    await ReplyErrorLocalizedAsync("crypto_not_found").ConfigureAwait(false);
                    return;
                }


                await ctx.Channel.EmbedAsync(new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle($"{crypto.Name} ({crypto.Symbol})")
                    .WithUrl($"https://coinmarketcap.com/currencies/{crypto.Slug}/")
                    .WithThumbnailUrl($"https://s2.coinmarketcap.com/static/img/coins/128x128/{crypto.Id}.png")
                    .AddField(GetText("market_cap"), $"${crypto.Quote.Usd.Market_Cap:n0}", true)
                    .AddField(GetText("price"), $"${crypto.Quote.Usd.Price}", true)
                    .AddField(GetText("volume_24h"), $"${crypto.Quote.Usd.Volume_24h:n0}", true)
                    .AddField(GetText("change_7d_24h"), $"{crypto.Quote.Usd.Percent_Change_7d}% / {crypto.Quote.Usd.Percent_Change_24h}%", true)
                    .WithImageUrl($"https://s2.coinmarketcap.com/generated/sparklines/web/7d/usd/{crypto.Id}.png")).ConfigureAwait(false);
            }
        }
    }
}
