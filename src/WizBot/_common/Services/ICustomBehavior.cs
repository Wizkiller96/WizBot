using WizBot.Common.ModuleBehaviors;

namespace WizBot.Services;

public interface ICustomBehavior
    : IExecOnMessage,
        IInputTransformer,
        IExecPreCommand,
        IExecNoCommand,
        IExecPostCommand
{

}