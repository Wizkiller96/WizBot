#nullable disable
using NadekoBot.Modules.Gambling.Services;

namespace NadekoBot.Common.TypeReaders;

public sealed class ShmartNumberTypeReader : NadekoTypeReader<ShmartNumber>
{
    private readonly BaseShmartInputAmountReader _tr;

    public ShmartNumberTypeReader(DbService db, GamblingConfigService gambling)
    {
        _tr = new BaseShmartInputAmountReader(db, gambling);
    }

    public override async ValueTask<TypeReaderResult<ShmartNumber>> ReadAsync(ICommandContext ctx, string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return TypeReaderResult.FromError<ShmartNumber>(CommandError.ParseFailed, "Input is empty.");

        var result = await _tr.ReadAsync(ctx, input);

        if (result.TryPickT0(out var val, out var err))
        {
            return TypeReaderResult.FromSuccess<ShmartNumber>(new(val));
        }

        return TypeReaderResult.FromError<ShmartNumber>(CommandError.Unsuccessful, err.Value);
    }
}