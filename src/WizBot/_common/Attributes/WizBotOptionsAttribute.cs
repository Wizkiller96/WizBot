namespace WizBot.Common.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class WizBotOptionsAttribute<TOption> : Attribute
    where TOption: IWizBotCommandOptions
{
}