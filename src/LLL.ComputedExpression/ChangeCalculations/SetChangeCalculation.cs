
namespace LLL.ComputedExpression.ChangeCalculations;

public class SetChangeCalculation<TElement>(
    bool incremental,
    IEqualityComparer<TElement> comparer
) : IChangeCalculation<IEnumerable<TElement>, SetChange<TElement>>
{
    public bool IsIncremental => incremental;
    public bool PreLoadEntities => true;

    public SetChange<TElement> GetChange(IComputedValues<IEnumerable<TElement>> computedValues)
    {
        var original = computedValues.GetOriginalValue();
        var current = computedValues.GetCurrentValue();

        return new SetChange<TElement>
        {
            Removed = (original ?? []).Except(current ?? [], comparer).ToArray(),
            Added = (current ?? []).Except(original ?? [], comparer).ToArray()
        };
    }

    public bool IsNoChange(SetChange<TElement> result)
    {
        return result.Removed.Count == 0 && result.Added.Count == 0;
    }

    public SetChange<TElement> CalculateDelta(SetChange<TElement> previous, SetChange<TElement> current)
    {
        return new SetChange<TElement>
        {
            Removed = (current.Removed ?? []).Except(previous.Removed ?? [], comparer)
                    .Concat((previous.Added ?? []).Except(current.Added ?? [], comparer))
                    .ToArray(),
            Added = (current.Added ?? []).Except(previous.Added ?? [], comparer)
                    .Concat((previous.Removed ?? []).Except(current.Removed ?? [], comparer))
                    .ToArray()
        };
    }

    public SetChange<TElement> AddDelta(SetChange<TElement> value, SetChange<TElement> delta)
    {
        return new SetChange<TElement>
        {
            Removed = value.Removed.Except(delta.Added).Concat(delta.Removed).ToArray(),
            Added = value.Added.Except(delta.Removed).Concat(delta.Added).ToArray()
        };
    }
}

public record SetChange<TElement>
{
    public required IReadOnlyCollection<TElement> Removed { get; init; }
    public required IReadOnlyCollection<TElement> Added { get; init; }
}