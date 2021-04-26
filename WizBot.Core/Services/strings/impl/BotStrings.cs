using System;
using System.Globalization;
using NLog;

namespace WizBot.Core.Services
{
    public class BotStrings : IBotStrings
    {
        /// <summary>
        /// Used as failsafe in case response key doesn't exist in the selected or default language.
        /// </summary>
        private readonly CultureInfo _usCultureInfo = new CultureInfo("en-US");
        private readonly ILocalization _localization;
        private readonly IBotStringsProvider _stringsProvider;
        private readonly Logger _log;

        public BotStrings(ILocalization loc, IBotStringsProvider stringsProvider)
        {
            _localization = loc;
            _stringsProvider = stringsProvider;
            _log = LogManager.GetCurrentClassLogger();
        }

        private string GetString(string key, CultureInfo cultureInfo)
            => _stringsProvider.GetText(cultureInfo.Name, key);

        public string GetText(string key, ulong? guildId = null, params object[] data)
            => GetText(key, _localization.GetCultureInfo(guildId), data);

        public string GetText(string key, CultureInfo cultureInfo)
        {
            var text = GetString(key, cultureInfo);

            if (string.IsNullOrWhiteSpace(text))
            {
                _log.Warn($"{key} key is missing from {cultureInfo} response strings. You may ignore this message.");
                text = GetString(key, _usCultureInfo) ?? $"Error: dkey {key} not found!";
                if (string.IsNullOrWhiteSpace(text))
                {
                    return
                        $"I can't tell you if the command is executed, because there was an error printing out the response." +
                        $" Key '{key}' is missing from resources. You may ignore this message.";
                }
            }
            return text;
        }

        public string GetText(string key, CultureInfo cultureInfo, params object[] data)
        {
            try
            {
                return string.Format(GetText(key, cultureInfo), data);
            }
            catch (FormatException)
            {
                return
                    $"I can't tell you if the command is executed, because there was an error printing out the response." +
                    $" Key '{key}' is not properly formatted. Please report this.";
            }
        }

        // public string[] GetUsages(string commandName, ulong guildId)
        //     => GetUsages(commandName, _localization.GetCultureInfo(guildId));

        public void Reload()
        {
            _stringsProvider.Reload();
        }
    }
}
