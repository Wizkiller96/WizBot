using NadekoBot.Common.Configs;

namespace NadekoBot.Services;

public sealed class ImagesConfig : ConfigServiceBase<ImageUrls>
{
    private const string PATH = "data/images.yml";

    private static readonly TypedKey<ImageUrls> _changeKey =
        new("config.images.updated");
    
    public override string Name
        => "images";

    public ImagesConfig(IConfigSeria serializer, IPubSub pubSub)
        : base(PATH, serializer, pubSub, _changeKey)
    {
    }
}

public interface IImageCache
{
    Task<byte[]?> GetHeadsImageAsync();
    Task<byte[]?> GetTailsImageAsync();
    Task<byte[]?> GetCurrencyImageAsync();
    Task<byte[]?> GetXpBackgroundImageAsync();
    Task<byte[]?> GetRategirlBgAsync();
    Task<byte[]?> GetRategirlDotAsync();
    Task<byte[]?> GetDiceAsync(int num);
    Task<byte[]?> GetSlotEmojiAsync(int number);
    Task<byte[]?> GetSlotBgAsync();
    Task<byte[]?> GetRipBgAsync();
    Task<byte[]?> GetRipOverlayAsync();
    Task<byte[]?> GetImageDataAsync(Uri url);
}

public sealed class ImageCache : IImageCache, INService
{
    private readonly IBotCache _cache;
    private readonly ImagesConfig _ic;
    private readonly Random _rng;
    private readonly IHttpClientFactory _httpFactory;

    public ImageCache(
        IBotCache cache,
        ImagesConfig ic,
        IHttpClientFactory httpFactory)
    {
        _cache = cache;
        _ic = ic;
        _httpFactory = httpFactory;
        _rng = new NadekoRandom();
    }

    private static TypedKey<byte[]> GetImageKey(Uri url)
        => new($"image:{url}");

    public async Task<byte[]?> GetImageDataAsync(Uri url)
        => await _cache.GetOrAddAsync(
            GetImageKey(url),
            async () =>
            {
                if (url.IsFile)
                {
                    return await File.ReadAllBytesAsync(url.LocalPath);
                }

                using var http = _httpFactory.CreateClient();
                var bytes = await http.GetByteArrayAsync(url);
                return bytes;
            },
            expiry: 48.Hours());

    private async Task<byte[]?> GetRandomImageDataAsync(Uri[] urls)
    {
        if (urls.Length == 0)
            return null;

        var url = urls[_rng.Next(0, urls.Length)];

        var data = await GetImageDataAsync(url);
        return data;
    }

    public Task<byte[]?> GetHeadsImageAsync()
        => GetRandomImageDataAsync(_ic.Data.Coins.Heads);

    public Task<byte[]?> GetTailsImageAsync()
        => GetRandomImageDataAsync(_ic.Data.Coins.Tails);

    public Task<byte[]?> GetCurrencyImageAsync()
        => GetRandomImageDataAsync(_ic.Data.Currency);

    public Task<byte[]?> GetXpBackgroundImageAsync()
        => GetImageDataAsync(_ic.Data.Xp.Bg);

    public Task<byte[]?> GetRategirlBgAsync()
        => GetImageDataAsync(_ic.Data.Rategirl.Matrix);

    public Task<byte[]?> GetRategirlDotAsync()
        => GetImageDataAsync(_ic.Data.Rategirl.Dot);

    public Task<byte[]?> GetDiceAsync(int num)
        => GetImageDataAsync(_ic.Data.Dice[num]);

    public Task<byte[]?> GetSlotEmojiAsync(int number)
        => GetImageDataAsync(_ic.Data.Slots.Emojis[number]);

    public Task<byte[]?> GetSlotBgAsync()
        => GetImageDataAsync(_ic.Data.Slots.Bg);

    public Task<byte[]?> GetRipBgAsync()
        => GetImageDataAsync(_ic.Data.Rip.Bg);

    public Task<byte[]?> GetRipOverlayAsync()
        => GetImageDataAsync(_ic.Data.Rip.Overlay);
}
