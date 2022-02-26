#nullable disable
using NadekoBot.Common.TypeReaders;
using NadekoBot.Modules.Permissions.Services;

namespace NadekoBot.Modules.Permissions;

public partial class Permissions
{
    [Group]
    public partial class GlobalPermissionCommands : NadekoModule
    {
        private readonly GlobalPermissionService _service;
        private readonly DbService _db;

        public GlobalPermissionCommands(GlobalPermissionService service, DbService db)
        {
            _service = service;
            _db = db;
        }

        [Cmd]
        [OwnerOnly]
        public async partial Task GlobalPermList()
        {
            var blockedModule = _service.BlockedModules;
            var blockedCommands = _service.BlockedCommands;
            if (!blockedModule.Any() && !blockedCommands.Any())
            {
                await ReplyErrorLocalizedAsync(strs.lgp_none);
                return;
            }

            var embed = _eb.Create().WithOkColor();

            if (blockedModule.Any())
                embed.AddField(GetText(strs.blocked_modules), string.Join("\n", _service.BlockedModules));

            if (blockedCommands.Any())
                embed.AddField(GetText(strs.blocked_commands), string.Join("\n", _service.BlockedCommands));

            await ctx.Channel.EmbedAsync(embed);
        }

        [Cmd]
        [OwnerOnly]
        public async partial Task GlobalModule(ModuleOrCrInfo module)
        {
            var moduleName = module.Name.ToLowerInvariant();

            var added = _service.ToggleModule(moduleName);

            if (added)
            {
                await ReplyConfirmLocalizedAsync(strs.gmod_add(Format.Bold(module.Name)));
                return;
            }

            await ReplyConfirmLocalizedAsync(strs.gmod_remove(Format.Bold(module.Name)));
        }

        [Cmd]
        [OwnerOnly]
        public async partial Task GlobalCommand(CommandOrCrInfo cmd)
        {
            var commandName = cmd.Name.ToLowerInvariant();
            var added = _service.ToggleCommand(commandName);

            if (added)
            {
                await ReplyConfirmLocalizedAsync(strs.gcmd_add(Format.Bold(cmd.Name)));
                return;
            }

            await ReplyConfirmLocalizedAsync(strs.gcmd_remove(Format.Bold(cmd.Name)));
        }
    }
}