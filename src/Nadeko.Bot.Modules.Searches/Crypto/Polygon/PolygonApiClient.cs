// using System.Net.Http.Json;
//
// namespace NadekoBot.Modules.Searches;
//
// public sealed class PolygonApiClient : IDisposable
// {
//     private const string BASE_URL = "https://api.polygon.io/v3";
//     
//     private readonly HttpClient _httpClient;
//     private readonly string _apiKey;
//
//     public PolygonApiClient(HttpClient httpClient, string apiKey)
//     {
//         _httpClient = httpClient;
//         _apiKey = apiKey;
//     }
//     
//     public async Task<IReadOnlyCollection<PolygonTickerData>> TickersAsync(string? ticker = null, string? query = null)
//     {
//         if (string.IsNullOrWhiteSpace(query))
//             query = null; 
//         
//         if(query is not null)
//             query = Uri.EscapeDataString(query);
//         
//         var requestString = $"{BASE_URL}/reference/tickers"
//                             + "?type=CS"
//                             + "&active=true"
//                             + "&order=asc"
//                             + "&limit=1000"
//                             + $"&apiKey={_apiKey}";
//
//         if (!string.IsNullOrWhiteSpace(ticker))
//             requestString += $"&ticker={ticker}";
//         
//         if (!string.IsNullOrWhiteSpace(query))
//             requestString += $"&search={query}";
//
//
//         var response = await _httpClient.GetFromJsonAsync<PolygonTickerResponse>(requestString);
//
//         if (response is null)
//             return Array.Empty<PolygonTickerData>();
//
//         return response.Results;
//     }
//
//     // public async Task<PolygonTickerDetailsV3> TickerDetailsV3Async(string ticker)
//     // {
//     //     return new();
//     // }
//
//     public void Dispose()
//         => _httpClient.Dispose();
// }