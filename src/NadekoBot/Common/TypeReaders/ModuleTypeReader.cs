using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot.Extensions;

namespace NadekoBot.Common.TypeReaders
{
    public sealed class ModuleTypeReader : NadekoTypeReader<ModuleInfo>
    {
        private readonly CommandService _cmds;

        public ModuleTypeReader(CommandService cmds)
        {
            _cmds = cmds;
        }

        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input)
        {
            input = input.ToUpperInvariant();
            var module = _cmds.Modules.GroupBy(m => m.GetTopLevelModule()).FirstOrDefault(m => m.Key.Name.ToUpperInvariant() == input)?.Key;
            if (module is null)
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "No such module found."));

            return Task.FromResult(TypeReaderResult.FromSuccess(module));
        }
    }

    public sealed class ModuleOrCrTypeReader : NadekoTypeReader<ModuleOrCrInfo>
    {
        private readonly CommandService _cmds;

        public ModuleOrCrTypeReader(CommandService cmds)
        {
            _cmds = cmds;
        }

        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input)
        {
            input = input.ToUpperInvariant();
            var module = _cmds.Modules.GroupBy(m => m.GetTopLevelModule()).FirstOrDefault(m => m.Key.Name.ToUpperInvariant() == input)?.Key;
            if (module is null && input != "ACTUALCUSTOMREACTIONS")
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "No such module found."));

            return Task.FromResult(TypeReaderResult.FromSuccess(new ModuleOrCrInfo
            {
                Name = input,
            }));
        }
    }

    public sealed class ModuleOrCrInfo
    {
        public string Name { get; set; }
    }
}
