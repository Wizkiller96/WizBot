using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = Discord.Color;

namespace NadekoBot.Extensions;

public static class ImagesharpExtensions
{
    /// <summary>
    ///     Adds fallback fonts to <see cref="TextOptions" />
    /// </summary>
    /// <param name="opts"><see cref="TextOptions" /> to which fallback fonts will be added to</param>
    /// <param name="fallback">List of fallback Font Families to add</param>
    /// <returns>The same <see cref="TextOptions" /> to allow chaining</returns>
    public static TextOptions WithFallbackFonts(this TextOptions opts, List<FontFamily> fallback)
    {
        foreach (var ff in fallback)
            opts.FallbackFonts.Add(ff);

        return opts;
    }

    /// <summary>
    ///     Adds fallback fonts to <see cref="TextGraphicsOptions" />
    /// </summary>
    /// <param name="opts"><see cref="TextGraphicsOptions" /> to which fallback fonts will be added to</param>
    /// <param name="fallback">List of fallback Font Families to add</param>
    /// <returns>The same <see cref="TextGraphicsOptions" /> to allow chaining</returns>
    public static TextGraphicsOptions WithFallbackFonts(this TextGraphicsOptions opts, List<FontFamily> fallback)
    {
        opts.TextOptions.WithFallbackFonts(fallback);
        return opts;
    }

    // https://github.com/SixLabors/Samples/blob/master/ImageSharp/AvatarWithRoundedCorner/Program.cs
    public static IImageProcessingContext ApplyRoundedCorners(this IImageProcessingContext ctx, float cornerRadius)
    {
        var size = ctx.GetCurrentSize();
        var corners = BuildCorners(size.Width, size.Height, cornerRadius);

        ctx.SetGraphicsOptions(new GraphicsOptions
        {
            Antialias = true,
            // enforces that any part of this shape that has color is punched out of the background
            AlphaCompositionMode = PixelAlphaCompositionMode.DestOut
        });

        foreach (var c in corners)
            ctx = ctx.Fill(SixLabors.ImageSharp.Color.Red, c);

        return ctx;
    }

    private static IPathCollection BuildCorners(int imageWidth, int imageHeight, float cornerRadius)
    {
        // first create a square
        var rect = new RectangularPolygon(-0.5f, -0.5f, cornerRadius, cornerRadius);

        // then cut out of the square a circle so we are left with a corner
        var cornerTopLeft = rect.Clip(new EllipsePolygon(cornerRadius - 0.5f, cornerRadius - 0.5f, cornerRadius));

        // corner is now a corner shape positions top left
        //lets make 3 more positioned correctly, we can do that by translating the original around the center of the image

        var rightPos = imageWidth - cornerTopLeft.Bounds.Width + 1;
        var bottomPos = imageHeight - cornerTopLeft.Bounds.Height + 1;

        // move it across the width of the image - the width of the shape
        var cornerTopRight = cornerTopLeft.RotateDegree(90).Translate(rightPos, 0);
        var cornerBottomLeft = cornerTopLeft.RotateDegree(-90).Translate(0, bottomPos);
        var cornerBottomRight = cornerTopLeft.RotateDegree(180).Translate(rightPos, bottomPos);

        return new PathCollection(cornerTopLeft, cornerBottomLeft, cornerTopRight, cornerBottomRight);
    }

    public static Color ToDiscordColor(this Rgba32 color)
        => new(color.R, color.G, color.B);

    public static MemoryStream ToStream(this Image<Rgba32> img, IImageFormat? format = null)
    {
        var imageStream = new MemoryStream();
        if (format?.Name == "GIF")
            img.SaveAsGif(imageStream);
        else
        {
            img.SaveAsPng(imageStream,
                new()
                {
                    ColorType = PngColorType.RgbWithAlpha,
                    CompressionLevel = PngCompressionLevel.BestCompression
                });
        }

        imageStream.Position = 0;
        return imageStream;
    }
}