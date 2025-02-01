
namespace LLL.AutoCompute.EFCore;

internal static class EnumerableExtensions
{
    public static IReadOnlyList<T> TopoSort<T>(
        this IEnumerable<T> source,
        Func<T, IEnumerable<T>> getDependencies)
        where T : notnull
    {
        var visited = new HashSet<T>();
        var result = new List<T>();

        // Visit item, adding their dependencies first
        void Visit(T item)
        {
            if (!visited.Add(item))
                return;

            foreach (var v in getDependencies(item))
                Visit(v);

            result.Add(item);
        }

        foreach (var item in source)
            Visit(item);

        return result;
    }

}