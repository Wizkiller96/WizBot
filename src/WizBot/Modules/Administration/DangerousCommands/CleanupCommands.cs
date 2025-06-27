using WizBot.Modules.Administration.DangerousCommands;

namespace WizBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class CleanupCommands : CleanupModuleBase
    {
        private readonly ICleanupService _svc;
        private readonly IBotCredsProvider _creds;

        public CleanupCommands(ICleanupService svc, IBotCredsProvider creds)
        {
            _svc = svc;
            _creds = creds;
        }

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