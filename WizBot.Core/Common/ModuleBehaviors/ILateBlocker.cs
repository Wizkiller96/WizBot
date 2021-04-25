using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace WizBot.Common.ModuleBehaviors
{
    public interface ILateBlocker
    {
        Task<bool> TryBlockLate(DiscordSocketClient client, ICommandContext context,
            string moduleName, CommandInfo command);
    }
}
