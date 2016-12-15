using Discord.Commands;
using WizBot.Services;
using NLog;

namespace WizBot.Modules
{
    public class DiscordModule
    {
        protected Logger _log { get; }
        protected string _prefix { get; }

        public DiscordModule()
        {
            string prefix;
            if (WizBot.ModulePrefixes.TryGetValue(this.GetType().Name, out prefix))
                _prefix = prefix;
            else
                _prefix = "?missing_prefix?";

            _log = LogManager.GetCurrentClassLogger();
        }
    }
}
