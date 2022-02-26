#nullable disable
using NadekoBot.Modules.Utility.Services;

namespace NadekoBot.Modules.Utility;

public partial class Utility
{
    [Group]
    public partial class UnitConverterCommands : NadekoModule<ConverterService>
    {
        [Cmd]
        public async partial Task ConvertList()
        {
            var units = _service.Units;

            var embed = _eb.Create().WithTitle(GetText(strs.convertlist)).WithOkColor();


            foreach (var g in units.GroupBy(x => x.UnitType))
            {
                embed.AddField(g.Key.ToTitleCase(),
                    string.Join(", ", g.Select(x => x.Triggers.FirstOrDefault()).OrderBy(x => x)));
            }

            await ctx.Channel.EmbedAsync(embed);
        }

        [Cmd]
        [Priority(0)]
        public async partial Task Convert(string origin, string target, decimal value)
        {
            var originUnit = _service.Units.FirstOrDefault(x
                => x.Triggers.Select(y => y.ToUpperInvariant()).Contains(origin.ToUpperInvariant()));
            var targetUnit = _service.Units.FirstOrDefault(x
                => x.Triggers.Select(y => y.ToUpperInvariant()).Contains(target.ToUpperInvariant()));
            if (originUnit is null || targetUnit is null)
            {
                await ReplyErrorLocalizedAsync(strs.convert_not_found(Format.Bold(origin), Format.Bold(target)));
                return;
            }

            if (originUnit.UnitType != targetUnit.UnitType)
            {
                await ReplyErrorLocalizedAsync(strs.convert_type_error(Format.Bold(originUnit.Triggers.First()),
                    Format.Bold(targetUnit.Triggers.First())));
                return;
            }

            decimal res;
            if (originUnit.Triggers == targetUnit.Triggers)
                res = value;
            else if (originUnit.UnitType == "temperature")
            {
                //don't really care too much about efficiency, so just convert to Kelvin, then to target
                switch (originUnit.Triggers.First().ToUpperInvariant())
                {
                    case "C":
                        res = value + 273.15m; //celcius!
                        break;
                    case "F":
                        res = (value + 459.67m) * (5m / 9m);
                        break;
                    default:
                        res = value;
                        break;
                }

                //from Kelvin to target
                switch (targetUnit.Triggers.First().ToUpperInvariant())
                {
                    case "C":
                        res -= 273.15m; //celcius!
                        break;
                    case "F":
                        res = (res * (9m / 5m)) - 459.67m;
                        break;
                }
            }
            else
            {
                if (originUnit.UnitType == "currency")
                    res = value * targetUnit.Modifier / originUnit.Modifier;
                else
                    res = value * originUnit.Modifier / targetUnit.Modifier;
            }

            res = Math.Round(res, 4);

            await SendConfirmAsync(GetText(strs.convert(value,
                originUnit.Triggers.Last(),
                res,
                targetUnit.Triggers.Last())));
        }
    }
}