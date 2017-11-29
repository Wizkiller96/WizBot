using CommandLine;

namespace WizBot.Core.Common
{
    public class OptionsParser
    {
        private static OptionsParser _instance = new OptionsParser();
        public static OptionsParser Default => _instance;

        static OptionsParser() { }

        public T ParseFrom<T>(T options, string[] args) where T : IWizBotCommandOptions
        {
            var res = Parser.Default.ParseArguments<T>(args);
            options = (T)res.MapResult(x => x, x => options);
            options.NormalizeOptions();
            return options;
        }
    }
}