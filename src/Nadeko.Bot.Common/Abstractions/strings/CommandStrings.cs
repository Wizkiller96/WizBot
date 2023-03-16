#nullable disable
using YamlDotNet.Serialization;

namespace NadekoBot.Services;

public sealed class CommandStrings
{
    [YamlMember(Alias = "desc")]
    public string Desc { get; set; }

    [YamlMember(Alias = "args")]
    public string[] Args { get; set; }
}