using System.Net.Http.Json;
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

    public Task<IReadOnlyCollection<SymbolData>> SearchSymbolAsync(string query)
    {
        return Task.FromResult<IReadOnlyCollection<SymbolData>>(Array.Empty<SymbolData>());
        
        // try
        // {
        //     query = Uri.EscapeDataString(query);
        //     using var http = _httpClientFactory.CreateClient();
        //     var response = await http.GetFromJsonAsync<FinnHubSearchResponse>($"https://finnhub.io/api/v1/search"
        //                                                                       + $"?q={query}"
        //                                                                       + $"&token=");
        //
        //     if (response is null)
        //         return Array.Empty<SymbolData>();
        //
        //     return response.Result
        //                    .Where(x => x.Type == "Common Stock")
        //                    .Select(static x => new SymbolData(x.Symbol, x.Description))
        //                    .ToList();
        // }
        // catch (Exception ex)
        // {
        //     Log.Warning(ex, "Error searching stock symbol: {ErrorMessage}", ex.Message);
        //     return Array.Empty<SymbolData>();
        // }
    }
}

public record SymbolData(string Symbol, string Description);