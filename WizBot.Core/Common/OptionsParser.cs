﻿using CommandLine;

namespace WizBot.Core.Common
{
    public static class OptionsParser
    {
        public static T ParseFrom<T>(string[] args) where T : IWizBotCommandOptions, new()
            => ParseFrom(new T(), args).Item1;

        public static (T, bool) ParseFrom<T>(T options, string[] args) where T : IWizBotCommandOptions
        {
            using (var p = new Parser(x =>
             {
                 x.HelpWriter = null;
             }))
            {
                var res = p.ParseArguments<T>(args);
                options = res.MapResult(x => x, x => options);
                options.NormalizeOptions();
                return (options, res.Tag == ParserResultType.Parsed);
            }
        }
    }
}
