using WizBot.Common.Configs;

namespace WizBot.Services;

public sealed class ImagesConfig : ConfigServiceBase<ImageUrls>
{
    private const string PATH = "data/images.yml";

    private static readonly TypedKey<ImageUrls> _changeKey =
        new("config.images.updated");
    
    public override string Name
        => "images";

    public ImagesConfig(IConfigSeria serializer, IPubSub pubSub)
        : base(PATH, serializer, pubSub, _changeKey)
    {
        Migrate();
    }

    private void Migrate()
    {
        if (data.Version < 10)
        {
            ModifyConfig(c =>
            {
                if(c.Xp.Bg.ToString().Contains("cdn.wizbot.cc"))
                    c.Xp.Bg = new("https://cdn.wizbot.cc/xp/bgs/v6.png");
                c.Version = 10;
            });
        }
    }
}