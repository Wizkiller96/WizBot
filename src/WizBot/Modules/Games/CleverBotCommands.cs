using Discord;
using Discord.Commands;
using WizBot.Services;
using System;
using System.Threading.Tasks;
using WizBot.Common.Attributes;
using WizBot.Modules.Games.Common;
using WizBot.Modules.Games.Services;

namespace WizBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class CleverBotCommands : WizBotSubmodule<ChatterBotService>
        {
            private readonly DbService _db;

            public CleverBotCommands(DbService db)
            {
                _db = db;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task Cleverbot()
            {
                var channel = (ITextChannel)Context.Channel;

                if (_service.ChatterBotGuilds.TryRemove(channel.Guild.Id, out Lazy<ChatterBotSession> throwaway))
                {
                    using (var uow = _db.UnitOfWork)
                    {
                        uow.GuildConfigs.SetCleverbotEnabled(Context.Guild.Id, false);
                        await uow.CompleteAsync().ConfigureAwait(false);
                    }
                    await ReplyConfirmLocalized("cleverbot_disabled").ConfigureAwait(false);
                    return;
                }

                _service.ChatterBotGuilds.TryAdd(channel.Guild.Id, new Lazy<ChatterBotSession>(() => new ChatterBotSession(Context.Guild.Id), true));

                using (var uow = _db.UnitOfWork)
                {
                    uow.GuildConfigs.SetCleverbotEnabled(Context.Guild.Id, true);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                await ReplyConfirmLocalized("cleverbot_enabled").ConfigureAwait(false);
            }
        }

       
    }
}