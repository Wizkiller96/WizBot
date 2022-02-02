#nullable disable
using NadekoBot.Db;
using NadekoBot.Modules.Gambling.Services;
using NCalc;
using System.Text.RegularExpressions;

namespace NadekoBot.Common.TypeReaders;

public sealed class ShmartNumberTypeReader : NadekoTypeReader<ShmartNumber>
{
    private static readonly Regex _percentRegex = new(@"^((?<num>100|\d{1,2})%)$", RegexOptions.Compiled);
    private readonly DbService _db;
    private readonly GamblingConfigService _gambling;

    public ShmartNumberTypeReader(DbService db, GamblingConfigService gambling)
    {
        _db = db;
        _gambling = gambling;
    }

    public override ValueTask<TypeReaderResult<ShmartNumber>> ReadAsync(ICommandContext context, string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new(TypeReaderResult.FromError<ShmartNumber>(CommandError.ParseFailed, "Input is empty."));

        var i = input.Trim().ToUpperInvariant();

        i = i.Replace("K", "000");

        //can't add m because it will conflict with max atm

        if (TryHandlePercentage(context, i, out var num))
            return new(TypeReaderResult.FromSuccess(new ShmartNumber(num, i)));
        try
        {
            var expr = new Expression(i, EvaluateOptions.IgnoreCase);
            expr.EvaluateParameter += (str, ev) => EvaluateParam(str, ev, context);
            var lon = (long)decimal.Parse(expr.Evaluate().ToString());
            return new(TypeReaderResult.FromSuccess(new ShmartNumber(lon, input)));
        }
        catch (Exception)
        {
            return ValueTask.FromResult(
                TypeReaderResult.FromError<ShmartNumber>(CommandError.ParseFailed, $"Invalid input: {input}"));
        }
    }

    private void EvaluateParam(string name, ParameterArgs args, ICommandContext ctx)
    {
        switch (name.ToUpperInvariant())
        {
            case "PI":
                args.Result = Math.PI;
                break;
            case "E":
                args.Result = Math.E;
                break;
            case "ALL":
            case "ALLIN":
                args.Result = Cur(ctx);
                break;
            case "HALF":
                args.Result = Cur(ctx) / 2;
                break;
            case "MAX":
                args.Result = Max(ctx);
                break;
        }
    }

    private long Cur(ICommandContext ctx)
    {
        using var uow = _db.GetDbContext();
        return uow.DiscordUser.GetUserCurrency(ctx.User.Id);
    }

    private long Max(ICommandContext ctx)
    {
        var settings = _gambling.Data;
        var max = settings.MaxBet;
        return max == 0 ? Cur(ctx) : max;
    }

    private bool TryHandlePercentage(ICommandContext ctx, string input, out long num)
    {
        num = 0;
        var m = _percentRegex.Match(input);
        if (m.Captures.Count != 0)
        {
            if (!long.TryParse(m.Groups["num"].ToString(), out var percent))
                return false;

            num = (long)(Cur(ctx) * (percent / 100.0f));
            return true;
        }

        return false;
    }
}