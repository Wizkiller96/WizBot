﻿#nullable disable
using NadekoBot.Modules.Utility.Common;
using Newtonsoft.Json;

namespace NadekoBot.Modules.Utility.Services;

public class ConverterService : INService
{
    public ConvertUnit[] Units =>
        _cache.Redis.GetDatabase()
            .StringGet("converter_units")
            .ToString()
            .MapJson<ConvertUnit[]>();

    private readonly Timer _currencyUpdater;
    private readonly TimeSpan _updateInterval = new(12, 0, 0);
    private readonly DbService _db;
    private readonly IDataCache _cache;
    private readonly IHttpClientFactory _httpFactory;

    public ConverterService(DiscordSocketClient client, DbService db,
        IDataCache cache, IHttpClientFactory factory)
    {
        _db = db;
        _cache = cache;
        _httpFactory = factory;

        if (client.ShardId == 0)
        {
            _currencyUpdater = new(async shouldLoad => await UpdateCurrency((bool)shouldLoad),
                client.ShardId == 0,
                TimeSpan.Zero,
                _updateInterval);
        }
    }

    private async Task<Rates> GetCurrencyRates()
    {
        using var http = _httpFactory.CreateClient();
        var res = await http.GetStringAsync("https://convertapi.nadeko.bot/latest");
        return JsonConvert.DeserializeObject<Rates>(res);
    }

    private async Task UpdateCurrency(bool shouldLoad)
    {
        try
        {
            var unitTypeString = "currency";
            if (shouldLoad)
            {
                var currencyRates = await GetCurrencyRates();
                var baseType = new ConvertUnit()
                {
                    Triggers = new[] { currencyRates.Base },
                    Modifier = decimal.One,
                    UnitType = unitTypeString
                };
                var range = currencyRates.ConversionRates.Select(u => new ConvertUnit()
                {
                    Triggers = new[] { u.Key },
                    Modifier = u.Value,
                    UnitType = unitTypeString
                }).ToArray();

                var fileData = JsonConvert.DeserializeObject<ConvertUnit[]>(
                        File.ReadAllText("data/units.json"))
                    .Where(x => x.UnitType != "currency");

                var data = JsonConvert.SerializeObject(range.Append(baseType).Concat(fileData).ToList());
                _cache.Redis.GetDatabase()
                    .StringSet("converter_units", data);
            }
        }
        catch
        {
            // ignored
        }
    }
}

public class Rates
{
    public string Base { get; set; }
    public DateTime Date { get; set; }
    [JsonProperty("rates")]
    public Dictionary<string, decimal> ConversionRates { get; set; }
}
