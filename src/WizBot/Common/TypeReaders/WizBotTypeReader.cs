using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace WizBot.Common.TypeReaders
{
    public abstract class WizBotTypeReader<T> : TypeReader
    {
        public abstract Task<TypeReaderResult> ReadAsync(ICommandContext ctx, string input);

        public override Task<TypeReaderResult> ReadAsync(ICommandContext ctx, string input, IServiceProvider services)
            => ReadAsync(ctx, input);
    }
}
