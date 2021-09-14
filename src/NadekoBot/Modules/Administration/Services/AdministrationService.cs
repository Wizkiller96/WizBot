using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common.Collections;
using NadekoBot.Common.Replacements;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NadekoBot.Extensions;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Db;

namespace NadekoBot.Modules.Administration.Services
{
    public class AdministrationService : INService
    {
        public ConcurrentHashSet<ulong> DeleteMessagesOnCommand { get; }
        public ConcurrentDictionary<ulong, bool> DeleteMessagesOnCommandChannels { get; }

        private readonly DbService _db;
        private readonly ILogCommandService _logService;

        public AdministrationService(Bot bot, CommandHandler cmdHandler, DbService db, ILogCommandService logService)
        {
            _db = db;
            _logService = logService;

            DeleteMessagesOnCommand = new ConcurrentHashSet<ulong>(bot.AllGuildConfigs
                .Where(g => g.DeleteMessageOnCommand)
                .Select(g => g.GuildId));

            DeleteMessagesOnCommandChannels = new ConcurrentDictionary<ulong, bool>(bot.AllGuildConfigs
                .SelectMany(x => x.DelMsgOnCmdChannels)
                .ToDictionary(x => x.ChannelId, x => x.State)
                .ToConcurrent());

            cmdHandler.CommandExecuted += DelMsgOnCmd_Handler;
        }

        public (bool DelMsgOnCmd, IEnumerable<DelMsgOnCmdChannel> channels) GetDelMsgOnCmdData(ulong guildId)
        {
            using (var uow = _db.GetDbContext())
            {
                var conf = uow.GuildConfigsForId(guildId,
                    set => set.Include(x => x.DelMsgOnCmdChannels));

                return (conf.DeleteMessageOnCommand, conf.DelMsgOnCmdChannels);
            }
        }

        private Task DelMsgOnCmd_Handler(IUserMessage msg, CommandInfo cmd)
        {
            var _ = Task.Run(async () =>
            {
                if (!(msg.Channel is SocketTextChannel channel))
                    return;

                //wat ?!
                if (DeleteMessagesOnCommandChannels.TryGetValue(channel.Id, out var state))
                {
                    if (state && cmd.Name != "prune" && cmd.Name != "pick")
                    {
                        _logService.AddDeleteIgnore(msg.Id);
                        try { await msg.DeleteAsync().ConfigureAwait(false); } catch { }
                    }
                    //if state is false, that means do not do it
                }
                else if (DeleteMessagesOnCommand.Contains(channel.Guild.Id) && cmd.Name != "prune" && cmd.Name != "pick")
                {
                    _logService.AddDeleteIgnore(msg.Id);
                    try { await msg.DeleteAsync().ConfigureAwait(false); } catch { }
                }
            });
            return Task.CompletedTask;
        }

        public bool ToggleDeleteMessageOnCommand(ulong guildId)
        {
            bool enabled;
            using (var uow = _db.GetDbContext())
            {
                var conf = uow.GuildConfigsForId(guildId, set => set);
                enabled = conf.DeleteMessageOnCommand = !conf.DeleteMessageOnCommand;

                uow.SaveChanges();
            }
            return enabled;
        }

        public async Task SetDelMsgOnCmdState(ulong guildId, ulong chId, Administration.State newState)
        {
            using (var uow = _db.GetDbContext())
            {
                var conf = uow.GuildConfigsForId(guildId,
                    set => set.Include(x => x.DelMsgOnCmdChannels));

                var old = conf.DelMsgOnCmdChannels.FirstOrDefault(x => x.ChannelId == chId);
                if (newState == Administration.State.Inherit)
                {
                    if (old is not null)
                    {
                        conf.DelMsgOnCmdChannels.Remove(old);
                        uow.Remove(old);
                    }
                }
                else
                {
                    if (old is null)
                    {
                        old = new DelMsgOnCmdChannel { ChannelId = chId };
                        conf.DelMsgOnCmdChannels.Add(old);
                    }

                    old.State = newState == Administration.State.Enable;
                    DeleteMessagesOnCommandChannels[chId] = newState == Administration.State.Enable;
                }

                await uow.SaveChangesAsync();
            }

            if (newState == Administration.State.Disable)
            {
            }
            else if (newState == Administration.State.Enable)
            {
                DeleteMessagesOnCommandChannels[chId] = true;
            }
            else
            {
                DeleteMessagesOnCommandChannels.TryRemove(chId, out var _);
            }
        }

        public async Task DeafenUsers(bool value, params IGuildUser[] users)
        {
            if (!users.Any())
                return;
            foreach (var u in users)
            {
                try
                {
                    await u.ModifyAsync(usr => usr.Deaf = value).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }
        }

        public async Task EditMessage(ICommandContext context, ITextChannel chanl, ulong messageId, string input)
        {
            var msg = await chanl.GetMessageAsync(messageId);

            if (!(msg is IUserMessage umsg) || msg.Author.Id != context.Client.CurrentUser.Id)
                return;

            var rep = new ReplacementBuilder()
                    .WithDefault(context)
                    .Build();

            var text = SmartText.CreateFrom(input);
            text = rep.Replace(text);

            await umsg.EditAsync(text);
        }
    }
}
