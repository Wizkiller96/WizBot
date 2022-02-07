using System.Net.Http.Json;
using System.Text.Json;
using YahooFinanceApi;

namespace NadekoBot.Modules.Searches;

public sealed class DefaultStockDataService : IStockDataService, INService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DefaultStockDataService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    
    public async Task<IReadOnlyCollection<StockData>> GetStockDataAsync(string query)
    {
        try
        {
            if (!query.IsAlphaNumeric())
                return Array.Empty<StockData>();

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
            
            return symbols
                   .Select(static x => x.Value)
                          .Select(static x => new StockData()
                          {
                              Name = x.LongName,
                              Ticker = x.Symbol,
                              Price = x.RegularMarketPrice,
                              Close = x.RegularMarketPreviousClose,
                              MarketCap = x.MarketCap,
                              Change50d = x.FiftyDayAverageChangePercent,
                              Change200d = x.TwoHundredDayAverageChangePercent,
                              DailyVolume = x.AverageDailyVolume10Day,
                              Exchange = x.FullExchangeName
                          })
                          .ToList();
        }
        catch (Exception ex)
        {
            // what the hell is this api exception
            Log.Warning(ex, "Error getting stock data: {ErrorMessage}", ex.Message);
            return Array.Empty<StockData>();
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

    // public async Task<IReadOnlyCollection<CandleData>> GetCandleDataAsync(string query)
    // {
    //     
    // }
}

public record CandleData();