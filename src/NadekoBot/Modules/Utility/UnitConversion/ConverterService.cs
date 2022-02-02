#nullable disable
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Modules.Utility.Common;
using Newtonsoft.Json;

namespace NadekoBot.Modules.Utility.Services;

public class ConverterService : INService, IReadyExecutor
{
    public ConvertUnit[] Units
        => _cache.Redis.GetDatabase().StringGet("converter_units").ToString().MapJson<ConvertUnit[]>();

    private readonly TimeSpan _updateInterval = new(12, 0, 0);
    private readonly DiscordSocketClient _client;
    private readonly IDataCache _cache;
    private readonly IHttpClientFactory _httpFactory;

    public ConverterService(
        DiscordSocketClient client,
        IDataCache cache,
        IHttpClientFactory factory)
    {
        _client = client;
        _cache = cache;
        _httpFactory = factory;
    }

    public async Task OnReadyAsync()
    {
        if (_client.ShardId != 0)
            return;

        using var timer = new PeriodicTimer(_updateInterval);
        do
        {
            try
            {
                await UpdateCurrency();
            }
            catch
            {
                // ignored
            }
        } while (await timer.WaitForNextTickAsync());
    }

    private async Task<Rates> GetCurrencyRates()
    {
        using var http = _httpFactory.CreateClient();
        var res = await http.GetStringAsync("https://convertapi.nadeko.bot/latest");
        return JsonConvert.DeserializeObject<Rates>(res);
    }

    private async Task UpdateCurrency()
    {
        var unitTypeString = "currency";
        var currencyRates = await GetCurrencyRates();
        var baseType = new ConvertUnit
        {
            Triggers = new[] { currencyRates.Base },
            Modifier = decimal.One,
            UnitType = unitTypeString
        };
        var range = currencyRates.ConversionRates.Select(u => new ConvertUnit
                                 {
                                     Triggers = new[] { u.Key },
                                     Modifier = u.Value,
                                     UnitType = unitTypeString
                                 })
                                 .ToArray();

        var fileData = JsonConvert.DeserializeObject<ConvertUnit[]>(File.ReadAllText("data/units.json"))
                                  ?.Where(x => x.UnitType != "currency");
        if (fileData is null)
            return;

        var data = JsonConvert.SerializeObject(range.Append(baseType).Concat(fileData).ToList());
        _cache.Redis.GetDatabase().StringSet("converter_units", data);
    }
}

public class Rates
{
    public string Base { get; set; }
    public DateTime Date { get; set; }

    [JsonProperty("rates")]
    public Dictionary<string, decimal> ConversionRates { get; set; }
}