using NadekoBot.Common.Configs;

namespace NadekoBot.Modules.Utility.Patronage;

public class PatronageConfig : ConfigServiceBase<PatronConfigData>
{
    public override string Name
        => "patron";

    private static readonly TypedKey<PatronConfigData> _changeKey
        = new TypedKey<PatronConfigData>("config.patron.updated");

    private const string FILE_PATH = "data/patron.yml";

    public PatronageConfig(IConfigSeria serializer, IPubSub pubSub) : base(FILE_PATH, serializer, pubSub, _changeKey)
    {
    }
}