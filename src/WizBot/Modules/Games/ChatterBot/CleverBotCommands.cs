#nullable disable
using WizBot.Db;
using WizBot.Modules.Games.Services;

namespace WizBot.Modules.Games;

public partial class Games
{
    [Group]
    public partial class ChatterBotCommands : WizBotModule<ChatterBotService>
    {
        private readonly DbService _db;

        public ChatterBotCommands(DbService db)
            => _db = db;

        [NoPublicBot]
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async partial Task Cleverbot()
        {
            var channel = (ITextChannel)ctx.Channel;

            if (_service.ChatterBotGuilds.TryRemove(channel.Guild.Id, out _))
            {
                await using (var uow = _db.GetDbContext())
                {
                    uow.GuildConfigs.SetCleverbotEnabled(ctx.Guild.Id, false);
                    await uow.SaveChangesAsync();
                }

                await ReplyConfirmLocalizedAsync(strs.cleverbot_disabled);
                return;
            }

            _service.ChatterBotGuilds.TryAdd(channel.Guild.Id, new(() => _service.CreateSession(), true));

            await using (var uow = _db.GetDbContext())
            {
                uow.GuildConfigs.SetCleverbotEnabled(ctx.Guild.Id, true);
                await uow.SaveChangesAsync();
            }

            await ReplyConfirmLocalizedAsync(strs.cleverbot_enabled);
        }
    }
}