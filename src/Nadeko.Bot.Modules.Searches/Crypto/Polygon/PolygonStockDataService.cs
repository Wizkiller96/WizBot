// namespace NadekoBot.Modules.Searches;
//
// public sealed class PolygonStockDataService : IStockDataService
// {
//     private readonly IHttpClientFactory _httpClientFactory;
//     private readonly IBotCredsProvider _credsProvider;
//
//     public PolygonStockDataService(IHttpClientFactory httpClientFactory, IBotCredsProvider credsProvider)
//     {
//         _httpClientFactory = httpClientFactory;
//         _credsProvider = credsProvider;
//     }
//
//     public async Task<IReadOnlyCollection<StockData>> GetStockDataAsync(string? query = null)
//     {
//         using var httpClient = _httpClientFactory.CreateClient();
//         using var client = new PolygonApiClient(httpClient, string.Empty);
//         var data = await client.TickersAsync(query: query);
//
//         return data.Map(static x => new StockData()
//         {
//             Name = x.Name,
//             Ticker = x.Ticker,
//         });
//     }
// }