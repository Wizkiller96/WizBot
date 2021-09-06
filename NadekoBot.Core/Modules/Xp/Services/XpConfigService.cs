using System;
using NadekoBot.Core.Common;
using NadekoBot.Core.Common.Configs;
using NadekoBot.Core.Services;

namespace NadekoBot.Modules.Xp.Services
{
    public sealed class XpConfigService : ConfigServiceBase<XpConfig>
    {
        public override string Name { get; } = "xp";
        private const string FilePath = "data/xp.yml";
        private static TypedKey<XpConfig> changeKey = new TypedKey<XpConfig>("config.xp.updated");

        public XpConfigService(IConfigSeria serializer, IPubSub pubSub) 
            : base(FilePath, serializer, pubSub, changeKey)
        {
            AddParsedProp("txt.cooldown", conf => conf.MessageXpCooldown, int.TryParse,
                ConfigPrinters.ToString, x => x > 0);
            AddParsedProp("txt.per_msg", conf => conf.XpPerMessage, int.TryParse,
                ConfigPrinters.ToString, x => x >= 0);
            AddParsedProp("txt.per_image", conf => conf.XpFromImage, int.TryParse,
                ConfigPrinters.ToString, x => x > 0);
            
            AddParsedProp("voice.per_minute", conf => conf.VoiceXpPerMinute, double.TryParse,
                ConfigPrinters.ToString, x => x >= 0);
            AddParsedProp("voice.max_minutes", conf => conf.VoiceMaxMinutes, int.TryParse,
                ConfigPrinters.ToString, x => x > 0);

            Migrate();
        }

        private void Migrate()
        {
            if (_data.Version <= 1)
            {
                ModifyConfig(c =>
                {
                    c.Version = 2;
                    c.XpFromImage = 0;
                });
            }
        }
    }
}