#nullable disable
using NadekoBot.Common.Configs;

namespace NadekoBot.Modules.Xp.Services;

public sealed class XpConfigService : ConfigServiceBase<XpConfig>
{
    private const string FILE_PATH = "data/xp.yml";
    private static readonly TypedKey<XpConfig> _changeKey = new("config.xp.updated");

    public override string Name
        => "xp";

    public XpConfigService(IConfigSeria serializer, IPubSub pubSub)
        : base(FILE_PATH, serializer, pubSub, _changeKey)
    {
        AddParsedProp("txt.cooldown",
            conf => conf.MessageXpCooldown,
            int.TryParse,
            ConfigPrinters.ToString,
            x => x > 0);
        AddParsedProp("txt.per_msg", conf => conf.XpPerMessage, int.TryParse, ConfigPrinters.ToString, x => x >= 0);
        AddParsedProp("txt.per_image", conf => conf.XpFromImage, int.TryParse, ConfigPrinters.ToString, x => x > 0);

        AddParsedProp("voice.per_minute",
            conf => conf.VoiceXpPerMinute,
            double.TryParse,
            ConfigPrinters.ToString,
            x => x >= 0);
        AddParsedProp("voice.max_minutes",
            conf => conf.VoiceMaxMinutes,
            int.TryParse,
            ConfigPrinters.ToString,
            x => x > 0);

        Migrate();
    }

    private void Migrate()
    {
        if (data.Version < 2)
        {
            ModifyConfig(c =>
            {
                c.Version = 2;
                c.XpFromImage = 0;
            });
        }
    }
}