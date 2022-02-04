#nullable disable
using NadekoBot.Modules.NadekoExpressions;

namespace NadekoBot.Common.TypeReaders;

public sealed class CommandTypeReader : NadekoTypeReader<CommandInfo>
{
    private readonly CommandService _cmds;
    private readonly CommandHandler _handler;

    public CommandTypeReader(CommandHandler handler, CommandService cmds)
    {
        _handler = handler;
        _cmds = cmds;
    }

    public override ValueTask<TypeReaderResult<CommandInfo>> ReadAsync(ICommandContext ctx, string input)
    {
        input = input.ToUpperInvariant();
        var prefix = _handler.GetPrefix(ctx.Guild);
        if (!input.StartsWith(prefix.ToUpperInvariant(), StringComparison.InvariantCulture))
            return new(TypeReaderResult.FromError<CommandInfo>(CommandError.ParseFailed, "No such command found."));

        input = input[prefix.Length..];

        var cmd = _cmds.Commands.FirstOrDefault(c => c.Aliases.Select(a => a.ToUpperInvariant()).Contains(input));
        if (cmd is null)
            return new(TypeReaderResult.FromError<CommandInfo>(CommandError.ParseFailed, "No such command found."));

        return new(TypeReaderResult.FromSuccess(cmd));
    }
}

public sealed class CommandOrCrTypeReader : NadekoTypeReader<CommandOrCrInfo>
{
    private readonly CommandService _cmds;
    private readonly CommandHandler _commandHandler;
    private readonly NadekoExpressionsService _exprs;

    public CommandOrCrTypeReader(CommandService cmds, NadekoExpressionsService exprs, CommandHandler commandHandler)
    {
        _cmds = cmds;
        _exprs = exprs;
        _commandHandler = commandHandler;
    }

    public override async ValueTask<TypeReaderResult<CommandOrCrInfo>> ReadAsync(ICommandContext ctx, string input)
    {
        input = input.ToUpperInvariant();

        if (_exprs.ExpressionExists(ctx.Guild?.Id, input))
            return TypeReaderResult.FromSuccess(new CommandOrCrInfo(input, CommandOrCrInfo.Type.Custom));

        var cmd = await new CommandTypeReader(_commandHandler, _cmds).ReadAsync(ctx, input);
        if (cmd.IsSuccess)
        {
            return TypeReaderResult.FromSuccess(new CommandOrCrInfo(((CommandInfo)cmd.Values.First().Value).Name,
                CommandOrCrInfo.Type.Normal));
        }

        return TypeReaderResult.FromError<CommandOrCrInfo>(CommandError.ParseFailed, "No such command or cr found.");
    }
}

public class CommandOrCrInfo
{
    public enum Type
    {
        Normal,
        Custom
    }

    public string Name { get; set; }
    public Type CmdType { get; set; }

    public bool IsCustom
        => CmdType == Type.Custom;

    public CommandOrCrInfo(string input, Type type)
    {
        Name = input;
        CmdType = type;
    }
}