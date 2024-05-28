using System.Runtime.CompilerServices;

namespace WizBot.Common.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class CmdAttribute : CommandAttribute
{
    public string MethodName { get; }

    public CmdAttribute([CallerMemberName] string memberName = "")
        : base(CommandNameLoadHelper.GetCommandNameFor(memberName))
    {
        MethodName = memberName.ToLowerInvariant();
        Aliases = CommandNameLoadHelper.GetAliasesFor(memberName);
        Remarks = memberName.ToLowerInvariant();
        Summary = memberName.ToLowerInvariant();
    }
}
