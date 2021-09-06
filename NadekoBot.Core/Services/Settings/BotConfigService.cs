using System;
using Discord;
using NadekoBot.Core.Common;
using NadekoBot.Core.Common.Configs;
using SixLabors.ImageSharp.PixelFormats;

namespace NadekoBot.Core.Services
{
    /// <summary>
    /// Settings service for bot-wide configuration.
    /// </summary>
    public sealed class BotConfigService : ConfigServiceBase<BotConfig>
    {
        public override string Name { get; } = "bot";
        
        private const string FilePath = "data/bot.yml";
        private static TypedKey<BotConfig> changeKey = new TypedKey<BotConfig>("config.bot.updated");
        
        public BotConfigService(IConfigSeria serializer, IPubSub pubSub)
            : base(FilePath, serializer, pubSub, changeKey)
        {
            AddParsedProp("color.ok", bs => bs.Color.Ok, Rgba32.TryParseHex, ConfigPrinters.Color);
            AddParsedProp("color.error", bs => bs.Color.Error, Rgba32.TryParseHex, ConfigPrinters.Color);
            AddParsedProp("color.pending", bs => bs.Color.Pending, Rgba32.TryParseHex, ConfigPrinters.Color);
            AddParsedProp("help.text", bs => bs.HelpText, ConfigParsers.String, ConfigPrinters.ToString);
            AddParsedProp("help.dmtext", bs => bs.DmHelpText, ConfigParsers.String, ConfigPrinters.ToString);
            AddParsedProp("console.type", bs => bs.ConsoleOutputType, Enum.TryParse, ConfigPrinters.ToString);
            AddParsedProp("locale", bs => bs.DefaultLocale, ConfigParsers.Culture, ConfigPrinters.Culture);
            AddParsedProp("prefix", bs => bs.Prefix, ConfigParsers.String, ConfigPrinters.ToString);
            
            UpdateColors();
        }

        private void UpdateColors()
        {
            var ok = _data.Color.Ok;
            var error = _data.Color.Error;
            var pend = _data.Color.Pending;
            // todo future remove these static props once cleanup is done
            NadekoBot.OkColor = new Color(ok.R, ok.G, ok.B);
            NadekoBot.ErrorColor = new Color(error.R, error.G, error.B);
            NadekoBot.PendingColor = new Color(pend.R, pend.G, pend.B);
        }
        
        protected override void OnStateUpdate()
        {
            UpdateColors();
        }
    }
}