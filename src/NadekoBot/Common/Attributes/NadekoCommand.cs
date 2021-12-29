using System.Runtime.CompilerServices;

namespace NadekoBot.Common.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class NadekoCommandAttribute : CommandAttribute
{
    public string MethodName { get; }

    public NadekoCommandAttribute([CallerMemberName] string memberName = "")
        : base(CommandNameLoadHelper.GetCommandNameFor(memberName))
        => MethodName = memberName.ToLowerInvariant();
}