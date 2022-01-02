using System.Runtime.CompilerServices;

namespace NadekoBot.Common.Attributes;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class NadekoModuleAttribute : GroupAttribute
{
    public NadekoModuleAttribute(string moduleName)
        : base(moduleName)
    {
    }
}

[AttributeUsage(AttributeTargets.Method)]
internal sealed class NadekoDescriptionAttribute : SummaryAttribute
{
    public NadekoDescriptionAttribute([CallerMemberName] string name = "")
        : base(name.ToLowerInvariant())
    {
    }
}

[AttributeUsage(AttributeTargets.Method)]
internal sealed class NadekoUsageAttribute : RemarksAttribute
{
    public NadekoUsageAttribute([CallerMemberName] string name = "")
        : base(name.ToLowerInvariant())
    {
    }
}