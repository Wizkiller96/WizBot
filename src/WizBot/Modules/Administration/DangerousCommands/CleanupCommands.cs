using WizBot.Modules.Administration.DangerousCommands;

namespace WizBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class CleanupCommands : CleanupModuleBase
    {
        private readonly ICleanupService _svc;

        public CleanupCommands(ICleanupService svc)
            => _svc = svc;

        [Cmd]
        [OwnerOnly]
        [RequireContext(ContextType.DM)]
        public async Task CleanupGuildData()
        {
            var result = await _svc.DeleteMissingGuildDataAsync();

            if (result is null)
            {
                await ctx.ErrorAsync();
                return;
            }

            await Response()
                  .Confirm($"{result.GuildCount} guilds' data remain in the database.")
                  .SendAsync();
        }
        
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task Keep()
        {
            var result = await _svc.KeepGuild(Context.Guild.Id);

            await Response().Text("This guild's bot data will be saved.").SendAsync();
        }
        
        [Cmd]
        [OwnerOnly]
        public async Task LeaveUnkeptServers(int shardId, int delay = 1000)
        {
            var keptGuildCount = await _svc.GetKeptGuildCount();

            var response = await PromptUserConfirmAsync(new EmbedBuilder()
                .WithDescription($"""
                                  Do you want the bot to leave all unkept servers?

                                  There are currently {keptGuildCount} kept servers.   

                                  **This is a highly destructive and irreversible action.**
                                  """));

            if (!response)
                return;

            await _svc.LeaveUnkeptServers(shardId, delay);
            await ctx.OkAsync();
        }
    }
}