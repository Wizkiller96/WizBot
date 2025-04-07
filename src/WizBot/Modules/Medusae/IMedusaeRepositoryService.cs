namespace WizBot.Modules;

public interface IMedusaeRepositoryService
{
    Task<List<ModuleItem>> GetModuleItemsAsync();
}