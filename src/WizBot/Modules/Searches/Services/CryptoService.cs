﻿using WizBot.Modules.Searches.Common;
using WizBot.Services;
using WizBot.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace WizBot.Modules.Searches.Services
{
    public class CryptoService : INService
    {
        private readonly IDataCache _cache;
        private readonly IHttpClientFactory _httpFactory;
        private readonly IBotCredentials _creds;
        
        public CryptoService(IDataCache cache, IHttpClientFactory httpFactory, IBotCredentials creds)
        {
            _cache = cache;
            _httpFactory = httpFactory;
            _creds = creds;
        }

        public async Task<(CryptoResponseData Data, CryptoResponseData Nearest)> GetCryptoData(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return (null, null);
            }

            name = name.ToUpperInvariant();
            var cryptos = await CryptoData().ConfigureAwait(false);

            if (cryptos is null)
                return (null, null);
            
            var crypto = cryptos
                ?.FirstOrDefault(x => x.Id.ToUpperInvariant() == name || x.Name.ToUpperInvariant() == name
                    || x.Symbol.ToUpperInvariant() == name);

            (CryptoResponseData Elem, int Distance)? nearest = null;
            if (crypto is null)
            {
                nearest = cryptos
                    .Select(x => (x, Distance: x.Name.ToUpperInvariant().LevenshteinDistance(name)))
                    .OrderBy(x => x.Distance)
                    .Where(x => x.Distance <= 2)
                    .FirstOrDefault();

                crypto = nearest?.Elem;
            }

            if (nearest != null)
            {
                return (null, crypto);
            }

            return (crypto, null);
        }

        private readonly SemaphoreSlim getCryptoLock = new SemaphoreSlim(1, 1);
        public async Task<List<CryptoResponseData>> CryptoData()
        {
            await getCryptoLock.WaitAsync();
            try
            {
                var fullStrData = await _cache.GetOrAddCachedDataAsync("wizbot:crypto_data", async _ =>
                {
                    try
                    {
                        using var _http = _httpFactory.CreateClient();
                        var strData = await _http.GetStringAsync(
                            $"https://pro-api.coinmarketcap.com/v1/cryptocurrency/listings/latest?" +
                            $"CMC_PRO_API_KEY={_creds.CoinmarketcapApiKey}" +
                            $"&start=1" +
                            $"&limit=5000" +
                            $"&convert=USD");

                        JsonConvert.DeserializeObject<CryptoResponse>(strData);

                        return strData;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error getting crypto data: {Message}", ex.Message);
                        return default;
                    }

                }, "", TimeSpan.FromHours(2));

                return JsonConvert.DeserializeObject<CryptoResponse>(fullStrData).Data;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retreiving crypto data: {Message}", ex.Message);
                return default;
            }
            finally
            {
                getCryptoLock.Release();
            }
        }
    }
}
