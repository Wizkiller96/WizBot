using CsvHelper;
using CsvHelper.Configuration;
using Google.Protobuf.WellKnownTypes;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

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

            using var http = _httpClientFactory.CreateClient();
            var data = await http.GetFromJsonAsync<YahooQueryModel>(
                $"https://query1.finance.yahoo.com/v7/finance/quote?symbols={query}");

            if (data is null)
                return default; 
            
            var symbol = data.QuoteResponse.Result.FirstOrDefault();

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

    private static CsvConfiguration _csvConfig = new(CultureInfo.InvariantCulture)
    {
        PrepareHeaderForMatch = args => args.Header.Humanize(LetterCasing.Title)
    };

    public async Task<IReadOnlyCollection<CandleData>> GetCandleDataAsync(string query)
    {
        using var http = _httpClientFactory.CreateClient();
        await using var resStream = await http.GetStreamAsync(
            $"https://query1.finance.yahoo.com/v7/finance/download/{query}"
            + $"?period1={DateTime.UtcNow.Subtract(30.Days()).ToTimestamp().Seconds}"
            + $"&period2={DateTime.UtcNow.ToTimestamp().Seconds}"
            + "&interval=1d");

        using var textReader = new StreamReader(resStream);
        using var csv = new CsvReader(textReader, _csvConfig);
        var records = csv.GetRecords<YahooFinanceCandleData>().ToArray();

        return records
            .Map(static x => new CandleData(x.Open, x.Close, x.High, x.Low, x.Volume));
    }
}