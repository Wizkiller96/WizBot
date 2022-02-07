#nullable disable
using NadekoBot.Modules.Searches.Services;
using System.Globalization;

namespace NadekoBot.Modules.Searches;

// todo weighted warnings fix
// todo autoplay/fairplay
public partial class Searches
{
    public partial class CryptoCommands : NadekoSubmodule<CryptoService>
    {
        private readonly IStockDataService _stocksService;

        public CryptoCommands(IStockDataService stocksService)
        {
            _stocksService = stocksService;
        }
        
        [Cmd]
        public async partial Task Stock([Leftover]string query)
        {
            using var typing = ctx.Channel.EnterTypingState();
            
            var stocks = await _stocksService.GetStockDataAsync(query);

            if (stocks.Count == 0)
            {
                var symbols = await _stocksService.SearchSymbolAsync(query);

                if (symbols.Count == 0)
                {
                    await ReplyErrorLocalizedAsync(strs.not_found);
                    return;
                }

                var symbol = symbols.First();
                var promptEmbed = _eb.Create()
                                     .WithDescription(symbol.Description)
                                     .WithTitle(GetText(strs.did_you_mean(symbol.Symbol)));
                
                if (!await PromptUserConfirmAsync(promptEmbed))
                    return;

                query = symbol.Symbol;
                stocks = await _stocksService.GetStockDataAsync(query);

                if (stocks.Count == 0)
                {
                    await ReplyErrorLocalizedAsync(strs.not_found);
                    return;
                }

            }
            
            // try to find a ticker match
            var stock = stocks.Count == 1
                ? stocks.FirstOrDefault()
                : stocks.FirstOrDefault(x => x.Ticker == query.ToUpperInvariant());
            
            if (stock is null)
            {
                var ebImprecise = _eb.Create()
                                     .WithOkColor()
                                     .WithTitle(GetText(strs.stocks_multiple_results))
                                     .WithDescription(stocks.Take(20)
                                                            .Select(s => $"{Format.Code(s.Ticker)} {s.Name.TrimTo(50)}")
                                                            .Join('\n'));

                await ctx.Channel.EmbedAsync(ebImprecise);
                return;
            }

            var localCulture = (CultureInfo)Culture.Clone();
            localCulture.NumberFormat.CurrencySymbol = "$";

            var sign = stock.Price >= stock.Close
                ? "\\🔼"
                : "\\🔻";

            var change = (stock.Price - stock.Close).ToString("N2", Culture);
            var changePercent = (1 - (stock.Close / stock.Price)).ToString("P1", Culture);
            
            var sign50 = stock.Change50d >= 0
                ? "\\🔼"
                : "\\🔻";

            var change50 = (stock.Change50d / 100).ToString("P1", Culture);
            
            var sign200 = stock.Change200d >= 0
                ? "\\🔼"
                : "\\🔻";
            
            var change200 = (stock.Change200d / 100).ToString("P1", Culture);
            
            var price = stock.Price.ToString("C2", localCulture);

            var eb = _eb.Create()
                        .WithOkColor()
                        .WithAuthor(stock.Ticker)
                        .WithTitle(stock.Name)
                        .AddField(GetText(strs.price), $"{sign} **{price}**", true)
                        .AddField(GetText(strs.market_cap), stock.MarketCap.ToString("C0", localCulture), true)
                        .AddField(GetText(strs.volume_24h), stock.DailyVolume.ToString("C0", localCulture), true)
                        .AddField("Change", $"{change} ({changePercent})", true)
                        .AddField("Change 50d", $"{sign50}{change50}", true)
                        .AddField("Change 200d", $"{sign200}{change200}", true)
                        .WithFooter(stock.Exchange);
            
            await ctx.Channel.EmbedAsync(eb);
        }
        

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

            var localCulture = (CultureInfo)Culture.Clone();
            localCulture.NumberFormat.CurrencySymbol = "$";
            
            var sevenDay = (usd.PercentChange7d / 100).ToString("P2", localCulture);
            var lastDay = (usd.PercentChange24h / 100).ToString("P2", localCulture);
            var price = usd.Price < 0.01
                ? usd.Price.ToString(localCulture)
                : usd.Price.ToString("C2", localCulture);

            var volume = usd.Volume24h.ToString("C0", localCulture);
            var marketCap = usd.MarketCap.ToString("C0", localCulture);
            var dominance = (usd.MarketCapDominance / 100).ToString("P2", localCulture);

            var toSend = _eb.Create()
                            .WithOkColor()
                            .WithAuthor($"#{crypto.CmcRank}")
                            .WithTitle($"{crypto.Name} ({crypto.Symbol})")
                            .WithUrl($"https://coinmarketcap.com/currencies/{crypto.Slug}/")
                            .WithThumbnailUrl( $"https://s3.coinmarketcap.com/static/img/coins/128x128/{crypto.Id}.png")
                            .AddField(GetText(strs.market_cap), marketCap, true)
                            .AddField(GetText(strs.price), price, true)
                            .AddField(GetText(strs.volume_24h), volume, true)
                            .AddField(GetText(strs.change_7d_24h), $"{sevenDay} / {lastDay}", true)
                            .AddField(GetText(strs.market_cap_dominance), dominance, true)
                            .WithImageUrl($"https://s3.coinmarketcap.com/generated/sparklines/web/7d/usd/{crypto.Id}.png");

            if (crypto.CirculatingSupply is double cs)
            {
                var csStr = cs.ToString("N0", localCulture);
                
                if (crypto.MaxSupply is double ms)
                {
                    var perc = (cs / ms).ToString("P1", localCulture);
                    
                    toSend.AddField(GetText(strs.circulating_supply), $"{csStr} ({perc})", true);
                }
                else
                {
                    toSend.AddField(GetText(strs.circulating_supply), csStr, true);
                }
            }
            
            
            await ctx.Channel.EmbedAsync(toSend);
        }
    }
}