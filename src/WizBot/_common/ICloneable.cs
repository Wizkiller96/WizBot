#nullable disable
namespace Wiz.Common;

public interface ICloneable<T>
    where T : new()
{
    public T Clone();
}