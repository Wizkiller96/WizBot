using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot.Services;
using NadekoBot.Modules.CustomReactions.Services;

namespace NadekoBot.Common.TypeReaders
{
    public sealed class CommandTypeReader : NadekoTypeReader<CommandInfo>
    {
        private readonly CommandHandler _handler;
        private readonly CommandService _cmds;

        public CommandTypeReader(CommandHandler handler, CommandService cmds)
        {
            _handler = handler;
            _cmds = cmds;
        }

        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input)
        {
            input = input.ToUpperInvariant();
            var prefix = _handler.GetPrefix(context.Guild);
            if (!input.StartsWith(prefix.ToUpperInvariant(), StringComparison.InvariantCulture))
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "No such command found."));

            input = input.Substring(prefix.Length);

            var cmd = _cmds.Commands.FirstOrDefault(c => c.Aliases.Select(a => a.ToUpperInvariant()).Contains(input));
            if (cmd is null)
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "No such command found."));

            return Task.FromResult(TypeReaderResult.FromSuccess(cmd));
        }
    }

    public sealed class CommandOrCrTypeReader : NadekoTypeReader<CommandOrCrInfo>
    {
        private readonly CommandService _cmds;
        private readonly CustomReactionsService _crs;
        private readonly CommandHandler _commandHandler;

        public CommandOrCrTypeReader(
            CommandService cmds,
            CustomReactionsService crs,
            CommandHandler commandHandler)
        {
            _cmds = cmds;
            _crs = crs;
            _commandHandler = commandHandler;
        }

        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input)
        {
            input = input.ToUpperInvariant();

            if (_crs.ReactionExists(context.Guild?.Id, input))
            {
                return TypeReaderResult.FromSuccess(new CommandOrCrInfo(input, CommandOrCrInfo.Type.Custom));
            }

            var cmd = await new CommandTypeReader(_commandHandler, _cmds).ReadAsync(context, input).ConfigureAwait(false);
            if (cmd.IsSuccess)
            {
                return TypeReaderResult.FromSuccess(new CommandOrCrInfo(((CommandInfo)cmd.Values.First().Value).Name, CommandOrCrInfo.Type.Normal));
            }
            return TypeReaderResult.FromError(CommandError.ParseFailed, "No such command or cr found.");
        }
    }

    public class CommandOrCrInfo
    {
        public enum Type
        {
            Normal,
            Custom,
        }

        public string Name { get; set; }
        public Type CmdType { get; set; }
        public bool IsCustom => CmdType == Type.Custom;

        public CommandOrCrInfo(string input, Type type)
        {
            this.Name = input;
            this.CmdType = type;
        }
    }
}
