using Discord.Commands;
using Discord.WebSocket;

namespace WizBot.Core.Common.TypeReaders
{
    public abstract class WizBotTypeReader<T> : TypeReader
    {
        protected readonly DiscordSocketClient _client;
        protected readonly CommandService _cmds;

        private WizBotTypeReader() { }
        public WizBotTypeReader(DiscordSocketClient client, CommandService cmds)
        {
            _client = client;
            _cmds = cmds;
        }
    }
}
