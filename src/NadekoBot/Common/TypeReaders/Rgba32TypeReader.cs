using Color = SixLabors.ImageSharp.Color;

#nullable disable
namespace NadekoBot.Common.TypeReaders;

public sealed class Rgba32TypeReader : NadekoTypeReader<Color>
{
    public override async ValueTask<TypeReaderResult<Color>> ReadAsync(ICommandContext context, string input)
    {
        await Task.Yield();

        input = input.Replace("#", "", StringComparison.InvariantCulture);
        try
        {
            return TypeReaderResult.FromSuccess(Color.ParseHex(input));
        }
        catch
        {
            return TypeReaderResult.FromError<Color>(CommandError.ParseFailed, "Parameter is not a valid color hex.");
        }
    }
}