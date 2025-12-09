
namespace LLL.AutoCompute.ChangeCalculators;

public record class SetChangeCalculator<TElement>(
    bool IsIncremental,
    IEqualityComparer<TElement> Comparer)
    : IChangeCalculator<IEnumerable<TElement>, SetChange<TElement>>
{
    public ComputedValueStrategy ValueStrategy => IsIncremental
        ? ComputedValueStrategy.Incremental
        : ComputedValueStrategy.Full;

    public SetChange<TElement> GetChange(IComputedValues<IEnumerable<TElement>> computedValues)
    {
        var original = computedValues.GetOriginalValue();
        var current = computedValues.GetCurrentValue();

        return new SetChange<TElement>
        {
            Removed = (original ?? []).Except(current ?? [], Comparer).ToArray(),
            Added = (current ?? []).Except(original ?? [], Comparer).ToArray()
        };
    }

    public bool IsNoChange(SetChange<TElement> result)
    {
        return result.Removed.Count == 0 && result.Added.Count == 0;
    }

    public SetChange<TElement> DeltaChange(SetChange<TElement> previous, SetChange<TElement> current)
    {
        return new SetChange<TElement>
        {
            Removed = (current.Removed ?? []).Except(previous.Removed ?? [], Comparer)
                    .Concat((previous.Added ?? []).Except(current.Added ?? [], Comparer))
                    .ToArray(),
            Added = (current.Added ?? []).Except(previous.Added ?? [], Comparer)
                    .Concat((previous.Removed ?? []).Except(current.Removed ?? [], Comparer))
                    .ToArray()
        };
    }

    public SetChange<TElement> ApplyChange(SetChange<TElement> value, SetChange<TElement> change)
    {
        return new SetChange<TElement>
        {
            Removed = value.Removed.Except(change.Added).Concat(change.Removed).ToArray(),
            Added = value.Added.Except(change.Removed).Concat(change.Added).ToArray()
        };
    }
}

public record SetChange<TElement>
{
    public required IReadOnlyCollection<TElement> Removed { get; init; }
    public required IReadOnlyCollection<TElement> Added { get; init; }
}
