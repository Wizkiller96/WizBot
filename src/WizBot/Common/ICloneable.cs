#nullable disable
namespace WizBot.Common;

public interface ICloneable<T>
    where T : new()
{
    public T Clone();
}