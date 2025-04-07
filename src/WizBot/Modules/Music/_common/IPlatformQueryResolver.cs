namespace WizBot.Modules.Music;

public interface IPlatformQueryResolver
{
    Task<ITrackInfo?> ResolveByQueryAsync(string query);
}