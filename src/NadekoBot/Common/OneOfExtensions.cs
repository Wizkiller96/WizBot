using OneOf.Types;
using OneOf;

namespace NadekoBot.Common;

public static class OneOfExtensions
{
    public static bool TryGetValue<T>(this OneOf<T, None> oneOf, out T value)
        => oneOf.TryPickT0(out value, out _);
}