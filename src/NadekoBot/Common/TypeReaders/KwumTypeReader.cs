using System.Threading.Tasks;
using Discord.Commands;

namespace NadekoBot.Common.TypeReaders
{
    public sealed class KwumTypeReader : NadekoTypeReader<kwum>
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input)
        {
            if (kwum.TryParse(input, out var val))
                return Task.FromResult(TypeReaderResult.FromSuccess(val));

            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Input is not a valid kwum"));
        }
    }

    public sealed class SmartTextTypeReader : NadekoTypeReader<SmartText>
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext ctx, string input)
        {
            return Task.FromResult(TypeReaderResult.FromSuccess(SmartText.CreateFrom(input)));
        }
    }
}