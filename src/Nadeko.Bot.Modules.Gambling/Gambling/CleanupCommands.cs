using Nadeko.Bot.Modules.Gambling.Gambling._Common;

namespace NadekoBot.Modules.Gambling;

public partial class Gambling
{
    [Group]
    public class CleanupCommands : CleanupModuleBase
    {
        private readonly IGamblingCleanupService _gcs;

        public CleanupCommands(IGamblingCleanupService gcs)
        {
            _gcs = gcs;
        }

        [Cmd]
        [OwnerOnly]
        public Task DeleteWaifus()
            => ConfirmActionInternalAsync("Delete Waifus", () => _gcs.DeleteWaifus());

        [Cmd]
        [OwnerOnly]
        public async Task DeleteWaifu(IUser user)
            => await DeleteWaifu(user.Id);

        [Cmd]
        [OwnerOnly]
        public Task DeleteWaifu(ulong userId)
            => ConfirmActionInternalAsync($"Delete Waifu {userId}", () => _gcs.DeleteWaifu(userId));
        
        
        [Cmd]
        [OwnerOnly]
        public Task DeleteCurrency()
            => ConfirmActionInternalAsync("Delete Currency", () => _gcs.DeleteCurrency());

    }
}