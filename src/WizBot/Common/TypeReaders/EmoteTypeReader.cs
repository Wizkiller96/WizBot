using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace WizBot.Common.TypeReaders
{
    public sealed class EmoteTypeReader : WizBotTypeReader<Emote>
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext ctx, string input)
        {
            if (!Emote.TryParse(input, out var emote))
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Input is not a valid emote"));

            return Task.FromResult(TypeReaderResult.FromSuccess(emote));
        }
    }
}