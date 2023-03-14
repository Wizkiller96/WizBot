#nullable disable
namespace NadekoBot.Services;

public interface IConfigMigrator
{
    public void EnsureMigrated();
}