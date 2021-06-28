using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace NadekoBot.Common.ModuleBehaviors
{
    public interface ILateBlocker
    {
        public int Priority { get; }
        
        Task<bool> TryBlockLate(ICommandContext context, string moduleName, CommandInfo command);
    }
}
