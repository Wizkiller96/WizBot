using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Services;
using Discord.WebSocket;
using Serilog;

namespace NadekoBot.Modules.Gambling.Services
{
    public class VoteModel
    {
        [JsonPropertyName("userId")]
        public ulong UserId { get; set; }
    }
    
    public class VoteRewardService : INService, IReadyExecutor
    {
        private readonly DiscordSocketClient _client;
        private readonly IBotCredentials _creds;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ICurrencyService _currencyService;
        private readonly GamblingConfigService _gamb;
        private HttpClient _http;

        public VoteRewardService(
            DiscordSocketClient client,
            IBotCredentials creds,
            IHttpClientFactory httpClientFactory,
            ICurrencyService currencyService,
            GamblingConfigService gamb)
        {
            _client = client;
            _creds = creds;
            _httpClientFactory = httpClientFactory;
            _currencyService = currencyService;
            _gamb = gamb;
        }
        
        public async Task OnReadyAsync()
        {
            if (_client.ShardId != 0)
                return;
            
            _http = new HttpClient(new HttpClientHandler()
            {
                AllowAutoRedirect = false,
                ServerCertificateCustomValidationCallback = delegate { return true; }
            });
            
            while (true)
            {
                await Task.Delay(30000);

                var topggKey = _creds.Votes?.TopggKey;
                var topggServiceUrl = _creds.Votes?.TopggServiceUrl;

                try
                {
                    if (!string.IsNullOrWhiteSpace(topggKey)
                        && !string.IsNullOrWhiteSpace(topggServiceUrl))
                    {
                        _http.DefaultRequestHeaders.Authorization = new(topggKey);
                        var uri = new Uri(new(topggServiceUrl), "topgg/new");
                        var res = await _http.GetStringAsync(uri);
                        var data = JsonSerializer.Deserialize<List<VoteModel>>(res);

                        if (data is { Count: > 0 })
                        {
                            var ids = data.Select(x => x.UserId).ToList();

                            await _currencyService.AddBulkAsync(ids,
                                data.Select(_ => "top.gg vote reward"),
                                data.Select(x => _gamb.Data.VoteReward),
                                true);

                            Log.Information("Rewarding {Count} top.gg voters", ids.Count());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Critical error loading top.gg vote rewards.");
                }

                var discordsKey = _creds.Votes?.DiscordsKey;
                var discordsServiceUrl = _creds.Votes?.DiscordsServiceUrl;
                
                try
                {
                    if (!string.IsNullOrWhiteSpace(discordsKey)
                        && !string.IsNullOrWhiteSpace(discordsServiceUrl))
                    {
                        _http.DefaultRequestHeaders.Authorization = new(discordsKey);
                        var res = await _http.GetStringAsync(new Uri(new(discordsServiceUrl), "discords/new"));
                        var data = JsonSerializer.Deserialize<List<VoteModel>>(res);

                        if (data is { Count: > 0 })
                        {
                            var ids = data.Select(x => x.UserId).ToList();
                            
                            await _currencyService.AddBulkAsync(ids,
                                data.Select(_ => "discords.com vote reward"),
                                data.Select(x => _gamb.Data.VoteReward),
                                true);

                            Log.Information("Rewarding {Count} discords.com voters", ids.Count());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Critical error loading discords.com vote rewards.");
                }
            }
        }
    }
}