﻿#nullable disable
using NCalc;
using System.Reflection;

namespace WizBot.Modules.Utility;

public partial class Utility
{
    [Group]
    public partial class CalcCommands : WizBotModule
    {
        [Cmd]
        public async Task Calculate([Leftover] string expression)
        {
            var expr = new Expression(expression, EvaluateOptions.IgnoreCase | EvaluateOptions.NoCache);
            expr.EvaluateParameter += Expr_EvaluateParameter;
            var result = expr.Evaluate();
            if (!expr.HasErrors())
                await SendConfirmAsync("⚙ " + GetText(strs.result), result.ToString());
            else
                await SendErrorAsync("⚙ " + GetText(strs.error), expr.Error);
        }

        private static void Expr_EvaluateParameter(string name, ParameterArgs args)
        {
            switch (name.ToLowerInvariant())
            {
                case "pi":
                    args.Result = Math.PI;
                    break;
                case "e":
                    args.Result = Math.E;
                    break;
            }
        }

        [Cmd]
        public async Task CalcOps()
        {
            var selection = typeof(Math).GetTypeInfo()
                                        .GetMethods()
                                        .DistinctBy(x => x.Name)
                                        .Select(x => x.Name)
                                        .Except(new[] { "ToString", "Equals", "GetHashCode", "GetType" });
            await SendConfirmAsync(GetText(strs.calcops(prefix)), string.Join(", ", selection));
        }
    }
}