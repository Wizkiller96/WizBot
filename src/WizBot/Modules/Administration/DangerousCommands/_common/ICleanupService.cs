namespace WizBot.Modules.Administration.DangerousCommands;

public interface ICleanupService
{
    Task<KeepResult?> DeleteMissingGuildDataAsync();
    Task<bool> KeepGuild(ulong guildId);
}