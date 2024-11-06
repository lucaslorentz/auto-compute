
namespace LLL.AutoCompute.ChangeCalculations;

public record class SetChangeCalculation<TElement>(
    bool incremental,
    IEqualityComparer<TElement>? comparer = null)
    : IChangeCalculation<IEnumerable<TElement>, SetChange<TElement>>
{
    public bool IsIncremental => incremental;
    public bool PreLoadEntities => true;
    public IEqualityComparer<TElement> Comparer {get;} = comparer ?? EqualityComparer<TElement>.Default;

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

    public SetChange<TElement> DeltaChange(SetChange<TElement> previous, SetChange<TElement> current)
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