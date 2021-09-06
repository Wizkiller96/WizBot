// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Net.Http;
// using System.Text.Json;
// using System.Text.Json.Serialization;
// using System.Text.RegularExpressions;
// using System.Threading.Tasks;
// using NadekoBot.Core.Services.Database.Models;
// using NadekoBot.Extensions;
// using Serilog;
// using JsonSerializer = System.Text.Json.JsonSerializer;
//
// namespace NadekoBot.Core.Modules.Searches.Common.StreamNotifications.Providers
// {
//     public sealed class TwitchHelixProvider : Provider
//     {
//         private readonly IHttpClientFactory _httpClientFactory;
//
//         //
//         private static Regex Regex { get; } = new Regex(@"twitch.tv/(?<name>.+[^/])/?",
//             RegexOptions.Compiled | RegexOptions.IgnoreCase);
//     
//         public override FollowedStream.FType Platform => FollowedStream.FType.Twitch;
//
//         private (string Token, DateTime Expiry) _token = default;
//     
//         public TwitchHelixProvider(IHttpClientFactory httpClientFactory)
//         {
//             _httpClientFactory = httpClientFactory;
//         }
//
//         private async Task EnsureTokenValidAsync()
//         {
//             if (_token != default && (DateTime.UtcNow - _token.Expiry) > TimeSpan.FromHours(1))
//                 return;
//
//             const string clientId = "";
//             const string clientSecret = "";
//             
//             var client = _httpClientFactory.CreateClient();
//             var res = await client.PostAsync("https://id.twitch.tv/oauth2/token" +
//                                         $"?client_id={clientId}" +
//                                         $"&client_secret={clientSecret}" +
//                                         "&grant_type=client_credentials", new StringContent(""));
//
//             var data = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;
//
//             _token = (data.GetProperty("access_token").GetString(),
//                 DateTime.UtcNow + TimeSpan.FromSeconds(data.GetProperty("expires_in").GetInt32()));
//
//         }
//     
//         public override Task<bool> IsValidUrl(string url)
//         {
//             var match = Regex.Match(url);
//             if (!match.Success)
//                 return Task.FromResult(false);
//     
//             var username = match.Groups["name"].Value;
//             return Task.FromResult(true);
//         }
//         
//         public override Task<StreamData?> GetStreamDataByUrlAsync(string url)
//         {
//             var match = Regex.Match(url);
//             if (match.Success)
//             {
//                 var name = match.Groups["name"].Value;
//                 return GetStreamDataAsync(name);
//             }
//     
//             return Task.FromResult<StreamData?>(null);
//         }
//     
//         public override async Task<StreamData?> GetStreamDataAsync(string id)
//         {
//             var data = await GetStreamDataAsync(new List<string> {id});
//     
//             return data.FirstOrDefault();
//         }
//     
//         public override async Task<List<StreamData>> GetStreamDataAsync(List<string> logins)
//         {
//             if (logins.Count == 0)
//                 return new List<StreamData>();
//
//             await EnsureTokenValidAsync();
//
//             using var http = _httpClientFactory.CreateClient();
//             http.DefaultRequestHeaders.Clear();
//             http.DefaultRequestHeaders.Add("client-id","67w6z9i09xv2uoojdm9l0wsyph4hxo6");
//             http.DefaultRequestHeaders.Add("Authorization",$"Bearer {_token.Token}");
//     
//             var res = new TwitchResponse()
//             {
//                 Data = new List<TwitchResponse.StreamApiData>()
//             };
//             foreach (var chunk in logins.Chunk(500))
//             {
//                 try
//                 {
//                     var str = await http.GetStringAsync($"https://api.twitch.tv/helix/streams" +
//                                                     $"?user_login={chunk.JoinWith(',')}" +
//                                                     $"&first=100");
//                     
//                     res = JsonSerializer.Deserialize<TwitchResponse>(str);
//                 }
//                 catch (Exception ex)
//                 {
//                     Log.Warning(ex, "Something went wrong retreiving {StreamPlatform} streams", Platform);
//                     return new List<StreamData>();
//                 }
//
//                 if (res.Data.Count == 0)
//                 {
//                     return new List<StreamData>();
//                 }
//             }
//
//             return res.Data.Select(ToStreamData).ToList();
//         }
//     
//         private StreamData ToStreamData(TwitchResponse.StreamApiData apiData)
//         {
//             return new StreamData()
//             {
//                 StreamType = FollowedStream.FType.Twitch,
//                 Name = apiData.UserName,
//                 UniqueName = apiData.UserId, 
//                 Viewers = apiData.ViewerCount,
//                 Title = apiData.Title,
//                 IsLive = apiData.Type == "live",
//                 Preview = apiData.ThumbnailUrl
//                     ?.Replace("{width}", "640")
//                     ?.Replace("{height}", "480"),
//                 Game = apiData.GameId,
//             };
//         }
//     }
//     
//     public class TwitchResponse
//     {
//         [JsonPropertyName("data")]
//         public List<StreamApiData> Data { get; set; }
//     
//         public class StreamApiData
//         {
//             [JsonPropertyName("id")]
//             public string Id { get; set; }
//             
//             [JsonPropertyName("user_id")]
//             public string UserId { get; set; }
//             
//             [JsonPropertyName("user_name")]
//             public string UserName { get; set; }
//             
//             [JsonPropertyName("game_id")]
//             public string GameId { get; set; }
//             
//             [JsonPropertyName("type")]
//             public string Type { get; set; }
//             
//             [JsonPropertyName("title")]
//             public string Title { get; set; }
//             
//             [JsonPropertyName("viewer_count")]
//             public int ViewerCount { get; set; }
//             
//             [JsonPropertyName("language")]
//             public string Language { get; set; }
//             
//             [JsonPropertyName("thumbnail_url")]
//             public string ThumbnailUrl { get; set; }
//             
//             [JsonPropertyName("started_at")]
//             public DateTime StartedAt { get; set; }
//         }
//     }
// }