#nullable disable
using WizBot.Db;
using WizBot.Modules.Games.Services;
using WizBot.Db.Models;

namespace WizBot.Modules.Games;

public partial class Games
{
    [Group]
    public partial class ChatterBotCommands : WizBotModule<ChatterBotService>
    {
        private readonly DbService _db;

        public ChatterBotCommands(DbService db)
            => _db = db;

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task Cleverbot()
        {
            var channel = (ITextChannel)ctx.Channel;

            if (_service.ChatterBotGuilds.TryRemove(channel.Guild.Id, out _))
            {
                await using (var uow = _db.GetDbContext())
                {
                    uow.Set<GuildConfig>().SetCleverbotEnabled(ctx.Guild.Id, false);
                    await uow.SaveChangesAsync();
                }

                await Response().Confirm(strs.cleverbot_disabled).SendAsync();
                return;
            }

            _service.ChatterBotGuilds.TryAdd(channel.Guild.Id, new(() => _service.CreateSession(), true));

            await using (var uow = _db.GetDbContext())
            {
                uow.Set<GuildConfig>().SetCleverbotEnabled(ctx.Guild.Id, true);
                await uow.SaveChangesAsync();
            }

            await Response().Confirm(strs.cleverbot_enabled).SendAsync();
        }
    }
}