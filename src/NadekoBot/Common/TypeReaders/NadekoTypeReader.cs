#nullable disable
namespace NadekoBot.Common.TypeReaders;

[MeansImplicitUse(ImplicitUseTargetFlags.Default | ImplicitUseTargetFlags.WithInheritors)]
public abstract class NadekoTypeReader<T> : TypeReader
{
    public abstract Task<TypeReaderResult> ReadAsync(ICommandContext ctx, string input);

    public override Task<TypeReaderResult> ReadAsync(ICommandContext ctx, string input, IServiceProvider services)
        => ReadAsync(ctx, input);
}