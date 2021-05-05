using System;
using System.Globalization;
using Discord;
using WizBot.Core.Common;
using WizBot.Core.Common.Configs;
using SixLabors.ImageSharp.PixelFormats;

namespace WizBot.Core.Services
{
    /// <summary>
    /// Settings service for bot-wide configuration.
    /// </summary>
    public class BotSettingsService : SettingsServiceBase<BotSettings>
    {
        private const string FilePath = "data/bot.yml";
        private static TypedKey<BotSettings> changeKey = new TypedKey<BotSettings>("config.bot.updated");

        public BotSettingsService(ISettingsSeria serializer, IPubSub pubSub)
            : base(FilePath, serializer, pubSub, changeKey)
        {
            AddParsedProp("color.ok", bs => bs.Color.Ok, Rgba32.TryParseHex, SettingPrinters.Color);
            AddParsedProp("color.error", bs => bs.Color.Error, Rgba32.TryParseHex, SettingPrinters.Color);
            AddParsedProp("color.pending", bs => bs.Color.Pending, Rgba32.TryParseHex, SettingPrinters.Color);
            AddParsedProp("help.text", bs => bs.HelpText, SettingParsers.String, SettingPrinters.ToString);
            AddParsedProp("help.dmtext", bs => bs.DmHelpText, SettingParsers.String, SettingPrinters.ToString);
            AddParsedProp("console.type", bs => bs.ConsoleOutputType, Enum.TryParse, SettingPrinters.ToString);
            AddParsedProp("locale", bs => bs.DefaultLocale, SettingParsers.Culture, SettingPrinters.Culture);
            AddParsedProp("prefix", bs => bs.Prefix, SettingParsers.String, SettingPrinters.ToString);

            UpdateColors();
        }

        private void UpdateColors()
        {
            var ok = _data.Color.Ok;
            var error = _data.Color.Error;
            // todo remove these static props once cleanup is done
            WizBot.OkColor = new Color(ok.R, ok.G, ok.B);
            WizBot.ErrorColor = new Color(error.R, error.G, error.B);
        }

        protected override void OnStateUpdate()
        {
            UpdateColors();
        }
    }
}