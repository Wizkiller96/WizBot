#nullable disable
namespace NadekoBot.Services;

public interface IBehaviourExecutor
{
    public Task<bool> RunEarlyBehavioursAsync(SocketGuild guild, IUserMessage usrMsg);
    public Task<string> RunInputTransformersAsync(SocketGuild guild, IUserMessage usrMsg);
    Task<bool> RunLateBlockersAsync(ICommandContext context, CommandInfo cmd);
    Task RunLateExecutorsAsync(SocketGuild guild, IUserMessage usrMsg);

    public void Initialize();
}