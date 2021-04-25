using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using WizBot.Common.Collections;
using WizBot.Common.ModuleBehaviors;
using WizBot.Core.Services;

namespace WizBot.Modules.Permissions.Services
{
    public class GlobalPermissionService : ILateBlocker, INService
    {
        public ConcurrentHashSet<string> BlockedModules { get; }
        public ConcurrentHashSet<string> BlockedCommands { get; }
        public int Priority { get; } = 0;

        public GlobalPermissionService(IBotConfigProvider bc)
        {
            BlockedModules = new ConcurrentHashSet<string>(bc.BotConfig.BlockedModules.Select(x => x.Name));
            BlockedCommands = new ConcurrentHashSet<string>(bc.BotConfig.BlockedCommands.Select(x => x.Name));
        }

        public async Task<bool> TryBlockLate(DiscordSocketClient client, ICommandContext ctx, string moduleName, CommandInfo command)
        {
            await Task.Yield();
            var commandName = command.Name.ToLowerInvariant();

            if (commandName != "resetglobalperms" &&
                (BlockedCommands.Contains(commandName) ||
                BlockedModules.Contains(moduleName.ToLowerInvariant())))
            {
                return true;
                //return new ExecuteCommandResult(cmd, null, SearchResult.FromError(CommandError.Exception, $"Command or module is blocked globally by the bot owner."));
            }
            return false;
        }
    }
}
