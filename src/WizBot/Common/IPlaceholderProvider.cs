#nullable disable
namespace WizBot.Common;

public interface IPlaceholderProvider
{
    public IEnumerable<(string Name, Func<string> Func)> GetPlaceholders();
}