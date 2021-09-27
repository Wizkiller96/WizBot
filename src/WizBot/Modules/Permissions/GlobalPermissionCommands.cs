using Discord;
using Discord.Commands;
using WizBot.Common.Attributes;
using WizBot.Common.TypeReaders;
using WizBot.Services;
using WizBot.Extensions;
using WizBot.Modules.Permissions.Services;
using System.Linq;
using System.Threading.Tasks;

namespace WizBot.Modules.Permissions
{
    public partial class Permissions
    {
        [Group]
        public class GlobalPermissionCommands : WizBotSubmodule
        {
            private GlobalPermissionService _service;
            private readonly DbService _db;

            public GlobalPermissionCommands(GlobalPermissionService service, DbService db)
            {
                _service = service;
                _db = db;
            }

            [WizBotCommand, Aliases]
            [OwnerOnly]
            public async Task GlobalPermList()
            {
                var blockedModule = _service.BlockedModules;
                var blockedCommands = _service.BlockedCommands;
                if (!blockedModule.Any() && !blockedCommands.Any())
                {
                    await ReplyErrorLocalizedAsync(strs.lgp_none).ConfigureAwait(false);
                    return;
                }

                var embed = _eb.Create().WithOkColor();

                if (blockedModule.Any())
                    embed.AddField(GetText(strs.blocked_modules)
                        , string.Join("\n", _service.BlockedModules)
                        , false);

                if (blockedCommands.Any())
                    embed.AddField(GetText(strs.blocked_commands)
                        , string.Join("\n", _service.BlockedCommands)
                        , false);

                await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [WizBotCommand, Aliases]
            [OwnerOnly]
            public async Task GlobalModule(ModuleOrCrInfo module)
            {
                var moduleName = module.Name.ToLowerInvariant();

                var added = _service.ToggleModule(moduleName);
                
                if (added)
                {
                    await ReplyConfirmLocalizedAsync(strs.gmod_add(Format.Bold(module.Name))).ConfigureAwait(false);
                    return;
                }
                
                await ReplyConfirmLocalizedAsync(strs.gmod_remove(Format.Bold(module.Name))).ConfigureAwait(false);
            }

            [WizBotCommand, Aliases]
            [OwnerOnly]
            public async Task GlobalCommand(CommandOrCrInfo cmd)
            {
                var commandName = cmd.Name.ToLowerInvariant();
                var added = _service.ToggleCommand(commandName);
                
                if (added)
                {
                    await ReplyConfirmLocalizedAsync(strs.gcmd_add(Format.Bold(cmd.Name))).ConfigureAwait(false);
                    return;
                }
                
                await ReplyConfirmLocalizedAsync(strs.gcmd_remove(Format.Bold(cmd.Name))).ConfigureAwait(false);
            }
        }
    }
}
