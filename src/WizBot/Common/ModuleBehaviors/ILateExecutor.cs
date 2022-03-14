namespace WizBot.Common.ModuleBehaviors;

/// <summary>
///     Last thing to be executed, won't stop further executions
/// </summary>
public interface ILateExecutor
{
    Task LateExecute(IGuild guild, IUserMessage msg);
}