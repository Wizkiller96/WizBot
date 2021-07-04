using Discord.Commands;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NadekoBot.Db;
using NadekoBot.Modules.Gambling.Services;
using NadekoBot.Services;

namespace NadekoBot.Common.TypeReaders
{
    public sealed class ShmartNumberTypeReader : NadekoTypeReader<ShmartNumber>
    {
        private readonly DbService _db;
        private readonly GamblingConfigService _gambling;

        public ShmartNumberTypeReader(DbService db, GamblingConfigService gambling)
        {
            _db = db;
            _gambling = gambling;
        }

        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input)
        {
            await Task.Yield();

            if (string.IsNullOrWhiteSpace(input))
                return TypeReaderResult.FromError(CommandError.ParseFailed, "Input is empty.");

            var i = input.Trim().ToUpperInvariant();

            i = i.Replace("K", "000");

            //can't add m because it will conflict with max atm

            if (TryHandlePercentage(context, i, out var num))
                return TypeReaderResult.FromSuccess(new ShmartNumber(num, i));
            try
            {
                var expr = new NCalc.Expression(i, NCalc.EvaluateOptions.IgnoreCase);
                expr.EvaluateParameter += (str, ev) => EvaluateParam(str, ev, context);
                var lon = (long)(decimal.Parse(expr.Evaluate().ToString()));
                return TypeReaderResult.FromSuccess(new ShmartNumber(lon, input));
            }
            catch (Exception)
            {
                return TypeReaderResult.FromError(CommandError.ParseFailed, $"Invalid input: {input}");
            }
        }

        private void EvaluateParam(string name, NCalc.ParameterArgs args, ICommandContext ctx)
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
                default:
                    break;
            }
        }

        private static readonly Regex percentRegex = new Regex(@"^((?<num>100|\d{1,2})%)$", RegexOptions.Compiled);

        private long Cur(ICommandContext ctx)
        {
            using var uow = _db.GetDbContext();
            return uow.DiscordUser.GetUserCurrency(ctx.User.Id);
        }

        private long Max(ICommandContext ctx)
        {
            var settings = _gambling.Data;
            var max = settings.MaxBet;
            return max == 0
                ? Cur(ctx)
                : max;
        }

        private bool TryHandlePercentage(ICommandContext ctx, string input, out long num)
        {
            num = 0;
            var m = percentRegex.Match(input);
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
}
