// ReSharper disable InconsistentNaming
#nullable disable
namespace WizBot.Modules.Utility;

public class EvalGlobals
{
    public ICommandContext ctx;
    public Utility.EvalCommands self;
    public IUser user;
    public IMessageChannel channel;
    public IGuild guild;
    public IServiceProvider services;
}