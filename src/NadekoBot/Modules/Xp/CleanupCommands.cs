namespace NadekoBot.Modules.Xp;

public sealed partial class Xp
{
    public class CleanupCommands : CleanupModuleBase
    {
        private readonly IXpCleanupService _service;

        public CleanupCommands(IXpCleanupService service)
        {
            _service = service;
        }

        [Cmd]
        [OwnerOnly]
        public Task DeleteXp()
            => ConfirmActionInternalAsync("Delete Xp", () => _service.DeleteXp());
    }
}