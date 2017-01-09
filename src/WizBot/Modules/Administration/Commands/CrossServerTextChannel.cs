using Discord;
using Discord.Commands;
using WizBot.Attributes;
using WizBot.Extensions;
using WizBot.Services;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace WizBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class CrossServerTextChannel : ModuleBase
        {
            static CrossServerTextChannel()
            {
                _log = LogManager.GetCurrentClassLogger();
                WizBot.Client.MessageReceived += async (imsg) =>
                {
                    try
                    {
                        if (imsg.Author.IsBot)
                            return;
                        var msg = imsg as IUserMessage;
                        if (msg == null)
                            return;
                        var channel = imsg.Channel as ITextChannel;
                        if (channel == null)
                            return;
                        if (msg.Author.Id == WizBot.Client.CurrentUser().Id) return;
                        foreach (var subscriber in Subscribers)
                        {
                            var set = subscriber.Value;
                            if (!set.Contains(channel))
                                continue;
                            foreach (var chan in set.Except(new[] { channel }))
                            {
                                try { await chan.SendMessageAsync(GetText(channel.Guild, channel, (IGuildUser)msg.Author, msg)).ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
                            }
                        }
                    }
                    catch (Exception ex) {
                        _log.Warn(ex);
                    }
                };
            }

            private static string GetText(IGuild server, ITextChannel channel, IGuildUser user, IUserMessage message) =>
                $"**{server.Name} | {channel.Name}** `{user.Username}`: " + message.Content;
            
            public static readonly ConcurrentDictionary<int, ConcurrentHashSet<ITextChannel>> Subscribers = new ConcurrentDictionary<int, ConcurrentHashSet<ITextChannel>>();
            private static Logger _log { get; }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task Scsc()
            {
                var token = new WizBotRandom().Next();
                var set = new ConcurrentHashSet<ITextChannel>();
                if (Subscribers.TryAdd(token, set))
                {
                    set.Add((ITextChannel)Context.Channel);
                    await ((IGuildUser)Context.User).SendConfirmAsync("This is your CSC token", token.ToString()).ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task Jcsc(int token)
            {
                ConcurrentHashSet<ITextChannel> set;
                if (!Subscribers.TryGetValue(token, out set))
                    return;
                set.Add((ITextChannel)Context.Channel);
                await Context.Channel.SendConfirmAsync("Joined cross server channel.").ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task Lcsc()
            {
                foreach (var subscriber in Subscribers)
                {
                    subscriber.Value.TryRemove((ITextChannel)Context.Channel);
                }
                await Context.Channel.SendMessageAsync("Left cross server channel.").ConfigureAwait(false);
            }
        }
    }
}