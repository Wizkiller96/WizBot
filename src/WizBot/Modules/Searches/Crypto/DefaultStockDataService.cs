using System.Text.Json;
using System.Text.Json.Serialization;
using YahooFinanceApi;

namespace WizBot.Modules.Searches;

public sealed class YahooStockClient
{
    private readonly IHttpClientFactory _clientFactory;

    public YahooStockClient(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }
}

public class YahooSymbolData
{
    [JsonPropertyName("language")]
    public string Language { get; set; }

    [JsonPropertyName("region")]
    public string Region { get; set; }

    [JsonPropertyName("quoteType")]
    public string QuoteType { get; set; }

    [JsonPropertyName("typeDisp")]
    public string TypeDisp { get; set; }

    [JsonPropertyName("quoteSourceName")]
    public string QuoteSourceName { get; set; }

    [JsonPropertyName("triggerable")]
    public bool Triggerable { get; set; }

    [JsonPropertyName("customPriceAlertConfidence")]
    public string CustomPriceAlertConfidence { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; }

    [JsonPropertyName("regularMarketOpen")]
    public double RegularMarketOpen { get; set; }

    [JsonPropertyName("averageDailyVolume3Month")]
    public int AverageDailyVolume3Month { get; set; }

    [JsonPropertyName("averageDailyVolume10Day")]
    public int AverageDailyVolume10Day { get; set; }

    [JsonPropertyName("fiftyTwoWeekLowChange")]
    public double FiftyTwoWeekLowChange { get; set; }

    [JsonPropertyName("fiftyTwoWeekLowChangePercent")]
    public double FiftyTwoWeekLowChangePercent { get; set; }

    [JsonPropertyName("fiftyTwoWeekRange")]
    public string FiftyTwoWeekRange { get; set; }

    [JsonPropertyName("fiftyTwoWeekHighChange")]
    public double FiftyTwoWeekHighChange { get; set; }

    [JsonPropertyName("fiftyTwoWeekHighChangePercent")]
    public double FiftyTwoWeekHighChangePercent { get; set; }

    [JsonPropertyName("fiftyTwoWeekLow")]
    public double FiftyTwoWeekLow { get; set; }

    [JsonPropertyName("fiftyTwoWeekHigh")]
    public double FiftyTwoWeekHigh { get; set; }

    [JsonPropertyName("fiftyDayAverage")]
    public double FiftyDayAverage { get; set; }

    [JsonPropertyName("fiftyDayAverageChange")]
    public double FiftyDayAverageChange { get; set; }

    [JsonPropertyName("fiftyDayAverageChangePercent")]
    public double FiftyDayAverageChangePercent { get; set; }

    [JsonPropertyName("twoHundredDayAverage")]
    public double TwoHundredDayAverage { get; set; }

    [JsonPropertyName("twoHundredDayAverageChange")]
    public double TwoHundredDayAverageChange { get; set; }

    [JsonPropertyName("twoHundredDayAverageChangePercent")]
    public double TwoHundredDayAverageChangePercent { get; set; }

    [JsonPropertyName("sourceInterval")]
    public int SourceInterval { get; set; }

    [JsonPropertyName("exchangeDataDelayedBy")]
    public int ExchangeDataDelayedBy { get; set; }

    [JsonPropertyName("tradeable")]
    public bool Tradeable { get; set; }

    [JsonPropertyName("marketState")]
    public string MarketState { get; set; }

    [JsonPropertyName("exchange")]
    public string Exchange { get; set; }

    [JsonPropertyName("shortName")]
    public string ShortName { get; set; }

    [JsonPropertyName("firstTradeDateMilliseconds")]
    public long FirstTradeDateMilliseconds { get; set; }

    [JsonPropertyName("priceHint")]
    public int PriceHint { get; set; }

    [JsonPropertyName("regularMarketChange")]
    public double RegularMarketChange { get; set; }

    [JsonPropertyName("regularMarketChangePercent")]
    public double RegularMarketChangePercent { get; set; }

    [JsonPropertyName("regularMarketTime")]
    public int RegularMarketTime { get; set; }

    [JsonPropertyName("regularMarketPrice")]
    public double RegularMarketPrice { get; set; }

    [JsonPropertyName("regularMarketDayHigh")]
    public double RegularMarketDayHigh { get; set; }

    [JsonPropertyName("regularMarketDayRange")]
    public string RegularMarketDayRange { get; set; }

    [JsonPropertyName("regularMarketDayLow")]
    public double RegularMarketDayLow { get; set; }

    [JsonPropertyName("regularMarketVolume")]
    public int RegularMarketVolume { get; set; }

    [JsonPropertyName("regularMarketPreviousClose")]
    public double RegularMarketPreviousClose { get; set; }

    [JsonPropertyName("bid")]
    public double Bid { get; set; }

    [JsonPropertyName("ask")]
    public double Ask { get; set; }

    [JsonPropertyName("bidSize")]
    public int BidSize { get; set; }

    [JsonPropertyName("askSize")]
    public int AskSize { get; set; }

    [JsonPropertyName("fullExchangeName")]
    public string FullExchangeName { get; set; }

    [JsonPropertyName("longName")]
    public string LongName { get; set; }

    [JsonPropertyName("messageBoardId")]
    public string MessageBoardId { get; set; }

    [JsonPropertyName("exchangeTimezoneName")]
    public string ExchangeTimezoneName { get; set; }

    [JsonPropertyName("exchangeTimezoneShortName")]
    public string ExchangeTimezoneShortName { get; set; }

    [JsonPropertyName("gmtOffSetMilliseconds")]
    public int GmtOffSetMilliseconds { get; set; }

    [JsonPropertyName("market")]
    public string Market { get; set; }

    [JsonPropertyName("esgPopulated")]
    public bool EsgPopulated { get; set; }

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; }
}

public class QuoteResponse
{
    [JsonPropertyName("result")]
    public List<YahooSymbolData> Result { get; set; }

    [JsonPropertyName("error")]
    public object Error { get; set; }
}

public class Root
{
    [JsonPropertyName("quoteResponse")]
    public QuoteResponse QuoteResponse { get; set; }
}

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