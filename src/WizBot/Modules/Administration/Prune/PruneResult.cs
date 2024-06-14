#nullable disable
namespace WizBot.Modules.Administration.Services;

public enum PruneResult
{
    Success,
    AlreadyRunning,
    FeatureLimit,
}