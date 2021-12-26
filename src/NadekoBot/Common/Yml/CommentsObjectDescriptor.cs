using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace NadekoBot.Common.Yml;

public sealed class CommentsObjectDescriptor : IObjectDescriptor
{
    private readonly IObjectDescriptor innerDescriptor;

    public CommentsObjectDescriptor(IObjectDescriptor innerDescriptor, string comment)
    {
        this.innerDescriptor = innerDescriptor;
        this.Comment = comment;
    }

    public string Comment { get; private set; }

    public object Value
        => innerDescriptor.Value;

    public Type Type
        => innerDescriptor.Type;

    public Type StaticType
        => innerDescriptor.StaticType;

    public ScalarStyle ScalarStyle
        => innerDescriptor.ScalarStyle;
}