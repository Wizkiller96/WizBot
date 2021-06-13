using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace WizBot.Core.Common.TypeReaders
{
    public class KwumTypeReader : WizBotTypeReader<kwum>
    {
        public KwumTypeReader(DiscordSocketClient client, CommandService cmds) : base(client, cmds)
        {
        }

        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            if (kwum.TryParse(input, out var val))
                return Task.FromResult(TypeReaderResult.FromSuccess(val));

            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Input is not a valid kwum"));
        }
    }
}