using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Extensions;
using Serilog;

namespace NadekoBot.Services
{
    public sealed class BehaviorExecutor : IBehaviourExecutor
    {
        private readonly DiscordSocketClient _client;
        private readonly IEnumerable<ILateExecutor> _lateExecutors;
        private readonly IEnumerable<ILateBlocker> _lateBlockers;
        private readonly IEnumerable<IEarlyBehavior> _earlyBehaviors;
        private readonly IEnumerable<IInputTransformer> _transformers;

        public BehaviorExecutor(
            DiscordSocketClient client,
            IEnumerable<ILateExecutor> lateExecutors,
            IEnumerable<ILateBlocker> lateBlockers,
            IEnumerable<IEarlyBehavior> earlyBehaviors,
            IEnumerable<IInputTransformer> transformers)
        {
            _client = client;
            _lateExecutors = lateExecutors;
            _lateBlockers = lateBlockers;
            _earlyBehaviors = earlyBehaviors;
            _transformers = transformers;
        }

        // todo early behaviors should print for themselves
        public async Task<bool> RunEarlyBehavioursAsync(SocketGuild guild, IUserMessage usrMsg)
        {
            foreach (var beh in _earlyBehaviors)
            {
                if (await beh.RunBehavior(guild, usrMsg))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<string> RunInputTransformersAsync(SocketGuild guild, IUserMessage usrMsg)
        {
            var messageContent = usrMsg.Content;
            foreach (var exec in _transformers)
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
            foreach (var exec in _lateBlockers)
            {
                if (await exec.TryBlockLate(ctx, cmd.Module.GetTopLevelModule().Name, cmd))
                {
                    Log.Information("Late blocking User [{0}] Command: [{1}] in [{2}]", 
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
            foreach (var exec in _lateExecutors)
            {
                try
                {
                    await exec.LateExecute(guild, usrMsg).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error in {TypeName} late executor: {ErrorMessage}",
                        exec.GetType().Name,
                        ex.Message);
                }
            }
        }
    }
}