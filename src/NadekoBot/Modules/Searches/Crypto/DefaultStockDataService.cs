using System.Text.Json;
using YahooFinanceApi;

namespace NadekoBot.Modules.Searches;

public sealed class DefaultStockDataService : IStockDataService, INService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DefaultStockDataService(IHttpClientFactory httpClientFactory)
        => _httpClientFactory = httpClientFactory;

    public async Task<StockData?> GetStockDataAsync(string query)
    {
        try
        {
            if (!query.IsAlphaNumeric())
                return default;

            var symbols = await Yahoo.Symbols(query)
                                     .Fields(Field.LongName,
                                         Field.Symbol,
                                         Field.RegularMarketPrice,
                                         Field.RegularMarketPreviousClose,
                                         Field.MarketCap,
                                         Field.FiftyDayAverageChangePercent,
                                         Field.TwoHundredDayAverageChangePercent,
                                         Field.AverageDailyVolume10Day,
                                         Field.FullExchangeName)
                                     .QueryAsync();

            var symbol = symbols.Values.FirstOrDefault();

            if (symbol is null)
                return default;
            
            return new()
            {
                Name = symbol.LongName,
                Symbol = symbol.Symbol,
                Price = symbol.RegularMarketPrice,
                Close = symbol.RegularMarketPreviousClose,
                MarketCap = symbol.MarketCap,
                Change50d = symbol.FiftyDayAverageChangePercent,
                Change200d = symbol.TwoHundredDayAverageChangePercent,
                DailyVolume = symbol.AverageDailyVolume10Day,
                Exchange = symbol.FullExchangeName
            };
        }
        catch (Exception)
        {
            // Log.Warning(ex, "Error getting stock data: {ErrorMessage}", ex.Message);
            return default;
        }
    }

    public async Task<IReadOnlyCollection<SymbolData>> SearchSymbolAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentNullException(nameof(query));

        query = Uri.EscapeDataString(query);
        
        using var http = _httpClientFactory.CreateClient();
        
        var res = await http.GetStringAsync(
            "https://finance.yahoo.com/_finance_doubledown/api/resource/searchassist"
            + $";searchTerm={query}"
            + "?device=console");

        var data = JsonSerializer.Deserialize<YahooFinanceSearchResponse>(res);

        if (data is null or { Items: null })
            return Array.Empty<SymbolData>();
        
        return data.Items
                  .Where(x => x.Type == "S")
                  .Select(x => new SymbolData(x.Symbol, x.Name))
                  .ToList();
    }

    public async Task<IReadOnlyCollection<CandleData>> GetCandleDataAsync(string query)
    {
        var candles = await Yahoo.GetHistoricalAsync(query, DateTime.Now.Subtract(30.Days()));

        return candles
            .Map(static x => new CandleData(x.Open, x.Close, x.High, x.Low, x.Volume));
    }
}