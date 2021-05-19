﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using WizBot.Common.ModuleBehaviors;
using WizBot.Core.Services;

namespace WizBot.Modules.Permissions.Services
{
    public class GlobalPermissionService : ILateBlocker, INService
    {
        private readonly BotConfigService _bss;
        public int Priority { get; } = 0;

        public HashSet<string> BlockedCommands => _bss.Data.Blocked.Commands;
        public HashSet<string> BlockedModules => _bss.Data.Blocked.Modules;

        public GlobalPermissionService(BotConfigService bss)
        {
            _bss = bss;
        }


        public Task<bool> TryBlockLate(DiscordSocketClient client, ICommandContext ctx, string moduleName, CommandInfo command)
        {
            var settings = _bss.Data;
            var commandName = command.Name.ToLowerInvariant();

            if (commandName != "resetglobalperms" &&
                (settings.Blocked.Commands.Contains(commandName) ||
                settings.Blocked.Modules.Contains(moduleName.ToLowerInvariant())))
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        /// <summary>
        /// Toggles module blacklist
        /// </summary>
        /// <param name="moduleName">Lowercase module name</param>
        /// <returns>Whether the module is added</returns>
        public bool ToggleModule(string moduleName)
        {
            var added = false;
            _bss.ModifyConfig(bs =>
            {
                if (bs.Blocked.Modules.Add(moduleName))
                {
                    added = true;
                }
                else
                {
                    bs.Blocked.Modules.Remove(moduleName);
                    added = false;
                }
            });

            return added;
        }

        /// <summary>
        /// Toggles command blacklist
        /// </summary>
        /// <param name="commandName">Lowercase command name</param>
        /// <returns>Whether the command is added</returns>
        public bool ToggleCommand(string commandName)
        {
            var added = false;
            _bss.ModifyConfig(bs =>
            {
                if (bs.Blocked.Commands.Add(commandName))
                {
                    added = true;
                }
                else
                {
                    bs.Blocked.Commands.Remove(commandName);
                    added = false;
                }
            });

            return added;
        }

        /// <summary>
        /// Resets all global permissions
        /// </summary>
        public Task Reset()
        {
            _bss.ModifyConfig(bs =>
            {
                bs.Blocked.Commands.Clear();
                bs.Blocked.Modules.Clear();
            });

            return Task.CompletedTask;
        }
    }
}
