#nullable disable
using Microsoft.Extensions.DependencyInjection;
using NadekoBot.Common.ModuleBehaviors;

namespace NadekoBot.Services;

public sealed class BehaviorExecutor : IBehaviourExecutor, INService
{
    private readonly IServiceProvider _services;
    private IEnumerable<ILateExecutor> lateExecutors;
    private IEnumerable<ILateBlocker> lateBlockers;
    private IEnumerable<IEarlyBehavior> earlyBehaviors;
    private IEnumerable<IInputTransformer> transformers;

    public BehaviorExecutor(IServiceProvider services)
        => _services = services;

    public void Initialize()
    {
        lateExecutors = _services.GetServices<ILateExecutor>();
        lateBlockers = _services.GetServices<ILateBlocker>();
        earlyBehaviors = _services.GetServices<IEarlyBehavior>().OrderByDescending(x => x.Priority);
        transformers = _services.GetServices<IInputTransformer>();
    }

    public async Task<bool> RunEarlyBehavioursAsync(SocketGuild guild, IUserMessage usrMsg)
    {
        foreach (var beh in earlyBehaviors)
        {
            if (await beh.RunBehavior(guild, usrMsg))
                return true;
        }

        return false;
    }

    public async Task<string> RunInputTransformersAsync(SocketGuild guild, IUserMessage usrMsg)
    {
        var messageContent = usrMsg.Content;
        foreach (var exec in transformers)
        {
            string newContent;
            if ((newContent = await exec.TransformInput(guild, usrMsg.Channel, usrMsg.Author, messageContent))
                != messageContent.ToLowerInvariant())
            {
                messageContent = newContent;
                break;
            }
        }

        return messageContent;
    }

    public async Task<bool> RunLateBlockersAsync(ICommandContext ctx, CommandInfo cmd)
    {
        foreach (var exec in lateBlockers)
        {
            if (await exec.TryBlockLate(ctx, cmd.Module.GetTopLevelModule().Name, cmd))
            {
                Log.Information("Late blocking User [{User}] Command: [{Command}] in [{Module}]",
                    ctx.User,
                    cmd.Aliases[0],
                    exec.GetType().Name);
                return true;
            }
        }

        return false;
    }

    public async Task RunLateExecutorsAsync(SocketGuild guild, IUserMessage usrMsg)
    {
        foreach (var exec in lateExecutors)
        {
            try
            {
                await exec.LateExecute(guild, usrMsg);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in {TypeName} late executor: {ErrorMessage}", exec.GetType().Name, ex.Message);
            }
        }
    }
}