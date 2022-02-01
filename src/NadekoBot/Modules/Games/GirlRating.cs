#nullable disable
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

namespace NadekoBot.Modules.Games.Common;

public class GirlRating
{
    public double Crazy { get; }
    public double Hot { get; }
    public int Roll { get; }
    public string Advice { get; }

    public AsyncLazy<Stream> Stream { get; }
    private readonly IImageCache _images;

    public GirlRating(
        IImageCache images,
        double crazy,
        double hot,
        int roll,
        string advice)
    {
        _images = images;
        Crazy = crazy;
        Hot = hot;
        Roll = roll;
        Advice = advice; // convenient to have it here, even though atm there are only few different ones.

        Stream = new(() =>
        {
            try
            {
                using var img = Image.Load(_images.RategirlMatrix);
                const int minx = 35;
                const int miny = 385;
                const int length = 345;

                var pointx = (int)(minx + (length * (Hot / 10)));
                var pointy = (int)(miny - (length * ((Crazy - 4) / 6)));

                using (var pointImg = Image.Load(_images.RategirlDot))
                {
                    img.Mutate(x => x.DrawImage(pointImg, new(pointx - 10, pointy - 10), new GraphicsOptions()));
                }

                var imgStream = new MemoryStream();
                img.SaveAsPng(imgStream);
                return imgStream;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error getting RateGirl image");
                return null;
            }
        });
    }
}