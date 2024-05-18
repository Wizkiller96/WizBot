namespace NadekoBot.Modules.Administration.DangerousCommands;

public partial class Administration 
{
    [Group]
    public class CleanupCommands : CleanupModuleBase
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
    }
}