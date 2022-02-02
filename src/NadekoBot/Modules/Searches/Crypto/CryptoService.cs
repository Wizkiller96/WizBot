#nullable disable
using NadekoBot.Modules.Searches.Common;
using System.Net.Http.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using StringExtensions = NadekoBot.Extensions.StringExtensions;

namespace NadekoBot.Modules.Searches.Services;

public class CryptoService : INService
{
    private readonly IDataCache _cache;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IBotCredentials _creds;

    private readonly SemaphoreSlim _getCryptoLock = new(1, 1);

    public CryptoService(IDataCache cache, IHttpClientFactory httpFactory, IBotCredentials creds)
    {
        _cache = cache;
        _httpFactory = httpFactory;
        _creds = creds;
    }

    public async Task<(CmcResponseData Data, CmcResponseData Nearest)> GetCryptoData(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (null, null);

        name = name.ToUpperInvariant();
        var cryptos = await GetCryptoDataInternal();

        if (cryptos is null)
            return (null, null);

        var crypto = cryptos?.FirstOrDefault(x
            => x.Slug.ToUpperInvariant() == name
               || x.Name.ToUpperInvariant() == name
               || x.Symbol.ToUpperInvariant() == name);

        if (crypto is not null)
            return (crypto, null);


        var nearest = cryptos
                      .Select(elem => (Elem: elem,
                          Distance: StringExtensions.LevenshteinDistance(elem.Name.ToUpperInvariant(), name)))
                      .OrderBy(x => x.Distance)
                      .FirstOrDefault(x => x.Distance <= 2);

        return (null, nearest.Elem);
    }

    public async Task<List<CmcResponseData>> GetCryptoDataInternal()
    {
        await _getCryptoLock.WaitAsync();
        try
        {
            var fullStrData = await _cache.GetOrAddCachedDataAsync("nadeko:crypto_data",
                async _ =>
                {
                    try
                    {
                        using var http = _httpFactory.CreateClient();
                        var strData = await http.GetFromJsonAsync<CryptoResponse>(
                            "https://pro-api.coinmarketcap.com/v1/cryptocurrency/listings/latest?"
                            + $"CMC_PRO_API_KEY={_creds.CoinmarketcapApiKey}"
                            + "&start=1"
                            + "&limit=5000"
                            + "&convert=USD");

                        return JsonSerializer.Serialize(strData);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error getting crypto data: {Message}", ex.Message);
                        return default;
                    }
                },
                "",
                TimeSpan.FromHours(2));

            return JsonSerializer.Deserialize<CryptoResponse>(fullStrData)?.Data ?? new();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retreiving crypto data: {Message}", ex.Message);
            return default;
        }
        finally
        {
            _getCryptoLock.Release();
        }
    }
}