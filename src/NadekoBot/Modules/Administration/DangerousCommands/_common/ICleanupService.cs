namespace NadekoBot.Modules.Administration.DangerousCommands;

public interface ICleanupService
{
    Task<KeepResult?> DeleteMissingGuildDataAsync();
}