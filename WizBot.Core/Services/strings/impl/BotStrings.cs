using System;
using System.Globalization;
using Serilog;
using YamlDotNet.Serialization;

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

        public BotStrings(ILocalization loc, IBotStringsProvider stringsProvider)
        {
            _localization = loc;
            _stringsProvider = stringsProvider;
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
                Log.Warning($"{key} key is missing from {cultureInfo} response strings. You may ignore this message.");
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
                Log.Warning($" Key '{key}' is not properly formatted in '{cultureInfo}' response strings. Please report this.");
                if (cultureInfo.Name != _usCultureInfo.Name)
                    return GetText(key, _usCultureInfo, data);
                return
                    $"I can't tell you if the command is executed, because there was an error printing out the response." +
                    $" Key '{key}' is not properly formatted. Please report this.";
            }
        }

        public CommandStrings GetCommandStrings(string commandName, ulong? guildId = null)
            => GetCommandStrings(commandName, _localization.GetCultureInfo(guildId));

        public CommandStrings GetCommandStrings(string commandName, CultureInfo cultureInfo)
        {
            var cmdStrings = _stringsProvider.GetCommandStrings(cultureInfo.Name, commandName);
            if (cmdStrings is null)
            {
                if (cultureInfo.Name == _usCultureInfo.Name
                    || (cmdStrings = _stringsProvider.GetCommandStrings(_usCultureInfo.Name, commandName)) == null)
                {
                    Log.Warning($"'{commandName}' doesn't exist in 'en-US' command strings. Please report this.");
                    return new CommandStrings()
                    {
                        Args = new[] { "" },
                        Desc = "?"
                    };
                }

                // Log.Warning($"'{commandName}' command strings don't exist in {cultureInfo.Name} culture." +
                //           $"This message is safe to ignore, however you can ask in WizBot support server how you can" +
                //           $" contribute command translations");
                return cmdStrings;
            }

            return cmdStrings;
        }

        public void Reload()
        {
            _stringsProvider.Reload();
        }
    }

    public class CommandStrings
    {
        [YamlMember(Alias = "desc")]
        public string Desc { get; set; }
        [YamlMember(Alias = "args")]
        public string[] Args { get; set; }
    }
}