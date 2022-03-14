using System.Runtime.CompilerServices;

namespace WizBot.Common.Attributes;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class WizBotModuleAttribute : GroupAttribute
{
    public WizBotModuleAttribute(string moduleName)
        : base(moduleName)
    {
    }
}

[AttributeUsage(AttributeTargets.Method)]
internal sealed class WizBotDescriptionAttribute : SummaryAttribute
{
    public WizBotDescriptionAttribute([CallerMemberName] string name = "")
        : base(name.ToLowerInvariant())
    {
    }
}

[AttributeUsage(AttributeTargets.Method)]
internal sealed class WizBotUsageAttribute : RemarksAttribute
{
    public WizBotUsageAttribute([CallerMemberName] string name = "")
        : base(name.ToLowerInvariant())
    {
    }
}