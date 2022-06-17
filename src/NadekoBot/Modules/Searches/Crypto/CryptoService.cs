#nullable enable
using NadekoBot.Modules.Searches.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Globalization;
using System.Net.Http.Json;
using System.Xml;
using Color = SixLabors.ImageSharp.Color;
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

    private PointF[] GetSparklinePointsFromSvgText(string svgText)
    {
        var xml = new XmlDocument();
        xml.LoadXml(svgText);

        var gElement = xml["svg"]?["g"];
        if (gElement is null)
            return Array.Empty<PointF>();

        Span<PointF> points = new PointF[gElement.ChildNodes.Count];
        var cnt = 0;

        bool GetValuesFromAttributes(XmlAttributeCollection attrs,
            out float x1,
            out float y1,
            out float x2,
            out float y2)
        {
            (x1, y1, x2, y2) = (0, 0, 0, 0);
            return attrs["x1"]?.Value is string x1Str
                   && float.TryParse(x1Str, NumberStyles.Any, CultureInfo.InvariantCulture, out x1)
                   && attrs["y1"]?.Value is string y1Str
                   && float.TryParse(y1Str, NumberStyles.Any, CultureInfo.InvariantCulture, out y1)
                   && attrs["x2"]?.Value is string x2Str
                   && float.TryParse(x2Str, NumberStyles.Any, CultureInfo.InvariantCulture, out x2)
                   && attrs["y2"]?.Value is string y2Str
                   && float.TryParse(y2Str, NumberStyles.Any, CultureInfo.InvariantCulture, out y2);
        }
        
        foreach (XmlElement x in gElement.ChildNodes)
        {
            if (x.Name != "line")
                continue;

            if (GetValuesFromAttributes(x.Attributes, out var x1, out var y1, out var x2, out var y2))
            {
                points[cnt++] = new(x1, y1);
                // this point will be set twice to the same value
                // on all points except the last one
                if(cnt + 1 < points.Length)
                    points[cnt + 1] = new(x2, y2);
            }
        }

        if (cnt == 0)
            return Array.Empty<PointF>();
        
        return points.Slice(0, cnt).ToArray();
    }
    
    private SixLabors.ImageSharp.Image<Rgba32> GenerateSparklineChart(PointF[] points, bool up)
    {
        const int width = 164;
        const int height = 48;
        
        var img = new Image<Rgba32>(width, height, Color.Transparent);
        var color = up
            ? Color.Green
            : Color.FromRgb(220, 0, 0);

        img.Mutate(x =>
        {
            x.DrawLines(color, 2, points);
        });
        
        return img;
    }
    
    public async Task<(CmcResponseData? Data, CmcResponseData? Nearest)> GetCryptoData(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (null, null);

        name = name.ToUpperInvariant();
        var cryptos = await GetCryptoDataInternal();

        if (cryptos is null or { Count: 0 })
            return (null, null);

        var crypto = cryptos.FirstOrDefault(x
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

    public async Task<List<CmcResponseData>?> GetCryptoDataInternal()
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

            if (fullStrData is null)
                return default;
            
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

    public async Task<Stream?> GetSparklineAsync(int id, bool up)
    {
        var key = $"crypto:sparkline:{id}";
        
        // attempt to get from cache
        var db = _cache.Redis.GetDatabase();
        byte[] bytes = await db.StringGetAsync(key);
        // if it succeeds, return it
        if (bytes is { Length: > 0 })
        {
            return bytes.ToStream();
        }
        
        // if it fails, generate a new one
        var points = await DownloadSparklinePointsAsync(id);
        if (points is null)
            return default;
        
        var sparkline = GenerateSparklineChart(points, up);

        // add to cache for 1h and return it
        
        var stream = sparkline.ToStream();
        await db.StringSetAsync(key, stream.ToArray(), expiry: TimeSpan.FromHours(1));
        return stream;
    }

    private async Task<PointF[]?> DownloadSparklinePointsAsync(int id)
    {
        try
        {
            using var http = _httpFactory.CreateClient();
            var str = await http.GetStringAsync(
                $"https://s3.coinmarketcap.com/generated/sparklines/web/7d/usd/{id}.svg");
            var points = GetSparklinePointsFromSvgText(str);
            return points;
        }
        catch(Exception ex)
        {
            Log.Warning(ex,
                "Exception occurred while downloading sparkline points: {ErrorMessage}",
                ex.Message);
            return default;
        }
    }
}