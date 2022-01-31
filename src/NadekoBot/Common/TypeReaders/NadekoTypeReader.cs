#nullable disable
namespace NadekoBot.Common.TypeReaders;

[MeansImplicitUse(ImplicitUseTargetFlags.Default | ImplicitUseTargetFlags.WithInheritors)]
public abstract class NadekoTypeReader<T> : TypeReader
{
    public abstract ValueTask<TypeReaderResult<T>> ReadAsync(ICommandContext ctx, string input);

    public override async Task<Discord.Commands.TypeReaderResult> ReadAsync(
        ICommandContext ctx,
        string input,
        IServiceProvider services)
        => await ReadAsync(ctx, input);
}

public static class TypeReaderResult
{
    public static TypeReaderResult<T> FromError<T>(CommandError error, string reason)
        => Discord.Commands.TypeReaderResult.FromError(error, reason);

    public static TypeReaderResult<T> FromSuccess<T>(in T value)
        => Discord.Commands.TypeReaderResult.FromSuccess(value);
}

public readonly struct TypeReaderResult<T>
{
    public bool IsSuccess
        => _result.IsSuccess;

    public IReadOnlyCollection<TypeReaderValue> Values
        => _result.Values;

    private readonly Discord.Commands.TypeReaderResult _result;

    private TypeReaderResult(in Discord.Commands.TypeReaderResult result)
        => _result = result;

    public static implicit operator TypeReaderResult<T>(in Discord.Commands.TypeReaderResult result)
        => new(result);

    public static implicit operator Discord.Commands.TypeReaderResult(in TypeReaderResult<T> wrapper)
        => wrapper._result;
}