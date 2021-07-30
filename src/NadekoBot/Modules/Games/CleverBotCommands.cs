using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Services;
using NadekoBot.Db;
using NadekoBot.Modules.Administration;
using NadekoBot.Modules.Games.Services;
using NadekoBot.Modules.Games.Common.ChatterBot;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class ChatterBotCommands : NadekoSubmodule<ChatterBotService>
        {
            private readonly DbService _db;

            public ChatterBotCommands(DbService db)
            {
                _db = db;
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageMessages)]
            public async Task Cleverbot()
            {
                var channel = (ITextChannel)ctx.Channel;

                if (_service.ChatterBotGuilds.TryRemove(channel.Guild.Id, out _))
                {
                    using (var uow = _db.GetDbContext())
                    {
                        uow.GuildConfigs.SetCleverbotEnabled(ctx.Guild.Id, false);
                        await uow.SaveChangesAsync();
                    }
                    await ReplyConfirmLocalizedAsync(strs.cleverbot_disabled).ConfigureAwait(false);
                    return;
                }

                _service.ChatterBotGuilds.TryAdd(channel.Guild.Id, new Lazy<IChatterBotSession>(() => _service.CreateSession(), true));

                using (var uow = _db.GetDbContext())
                {
                    uow.GuildConfigs.SetCleverbotEnabled(ctx.Guild.Id, true);
                    await uow.SaveChangesAsync();
                }

                await ReplyConfirmLocalizedAsync(strs.cleverbot_enabled).ConfigureAwait(false);
            }
        }

       
    }
}