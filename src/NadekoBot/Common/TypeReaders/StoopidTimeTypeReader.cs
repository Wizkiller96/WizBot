using Discord.Commands;
using NadekoBot.Common.TypeReaders.Models;
using System;
using System.Threading.Tasks;

namespace NadekoBot.Common.TypeReaders
{
    public sealed class StoopidTimeTypeReader : NadekoTypeReader<StoopidTime>
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Task.FromResult(TypeReaderResult.FromError(CommandError.Unsuccessful, "Input is empty."));
            try
            {
                var time = StoopidTime.FromInput(input);
                return Task.FromResult(TypeReaderResult.FromSuccess(time));
            }
            catch (Exception ex)
            {
                return Task.FromResult(TypeReaderResult.FromError(CommandError.Exception, ex.Message));
            }
        }
    }
}
