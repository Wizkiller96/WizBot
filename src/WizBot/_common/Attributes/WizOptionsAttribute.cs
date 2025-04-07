namespace WizBot.Common.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class WizOptionsAttribute<TOption> : Attribute
    where TOption: IWizCommandOptions
{
}