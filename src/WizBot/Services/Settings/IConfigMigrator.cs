#nullable disable
namespace WizBot.Services;

public interface IConfigMigrator
{
    public void EnsureMigrated();
}