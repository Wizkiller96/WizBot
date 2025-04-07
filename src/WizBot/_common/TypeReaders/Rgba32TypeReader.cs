using SixLabors.ImageSharp.PixelFormats;
using Color = SixLabors.ImageSharp.Color;

#nullable disable
namespace WizBot.Common.TypeReaders;

public sealed class Rgba32TypeReader : WizTypeReader<Rgba32>
{
    public override ValueTask<TypeReaderResult<Rgba32>> ReadAsync(ICommandContext context, string input)
    {
        if (!Color.TryParse(input, out var color))
        {
            Log.Information("Fail");
            return ValueTask.FromResult(
                TypeReaderResult.FromError<Rgba32>(CommandError.ParseFailed, "Parameter is not a valid color hex."));
        }

        return ValueTask.FromResult(TypeReaderResult.FromSuccess((Rgba32)color));
    }
}