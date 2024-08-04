
namespace LLL.AutoCompute.EFCore;

internal static class EnumerableExtensions
{
    public static IEnumerable<T> TopoSort<T>(
        this IReadOnlyList<T> source,
        Func<T, IEnumerable<T>> getDependencies)
        where T : notnull
    {
        var inDegrees = source.ToDictionary(x => x, x => 0);

        foreach (var item in source)
            foreach (var edge in getDependencies(item))
                inDegrees[edge]++;

        var result = new List<T>();
        var queue = new Queue<T>(inDegrees.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        while (queue.Count > 0)
        {
            var item = queue.Dequeue();
            result.Add(item);
            foreach (var edge in getDependencies(item))
            {
                inDegrees[edge]--;
                if (inDegrees[edge] == 0)
                    queue.Enqueue(edge);
            }
        }

        result.Reverse();

        if (result.Count != source.Count)
            throw new Exception("Cyclic dependencies found");

        return result;
    }
}