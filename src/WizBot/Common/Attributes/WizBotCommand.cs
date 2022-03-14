using System.Runtime.CompilerServices;

namespace WizBot.Common.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class WizBotCommandAttribute : CommandAttribute
{
    public string MethodName { get; }

    public WizBotCommandAttribute([CallerMemberName] string memberName = "")
        : base(CommandNameLoadHelper.GetCommandNameFor(memberName))
        => MethodName = memberName.ToLowerInvariant();
}