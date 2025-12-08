using System.Collections;

namespace LLL.AutoCompute.Internal;

public static class EnumerableExtensions
{
    public static Array ToArray(this IEnumerable source, Type type)
    {
        return (Array)typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray))!
            .MakeGenericMethod(type)
            .Invoke(null, [
                typeof(Enumerable).GetMethod(nameof(Enumerable.Cast))!
                    .MakeGenericMethod(type).Invoke(null, [source])
            ])!;
    }
}
