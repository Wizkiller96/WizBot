#nullable disable
using WizBot.Common.TypeReaders;
using static WizBot.Common.TypeReaders.TypeReaderResult;

namespace WizBot.Modules.Permissions;

public class CleverbotResponseCmdCdTypeReader : WizBotTypeReader<CleverBotResponseStr>
{
    public override ValueTask<TypeReaderResult<CleverBotResponseStr>> ReadAsync(
        ICommandContext ctx,
        string input)
        => input.ToLowerInvariant() == CleverBotResponseStr.CLEVERBOT_RESPONSE
            ? new(FromSuccess(new CleverBotResponseStr()))
            : new(FromError<CleverBotResponseStr>(CommandError.ParseFailed, "Not a valid cleverbot"));
}