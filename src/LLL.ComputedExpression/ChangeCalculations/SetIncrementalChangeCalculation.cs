
namespace LLL.ComputedExpression.ChangeCalculations;

public class SetChangeCalculation<TElement>(
    IEqualityComparer<TElement> comparer
) : IncrementalChangeCalculation<IEnumerable<TElement>, SetChange<TElement>>
{
    protected override SetChange<TElement> CalculateChange(IEnumerable<TElement> original, IEnumerable<TElement> current)
    {
        return new SetChange<TElement>
        {
            Removed = original.Except(current, comparer).ToArray(),
            Added = current.Except(original, comparer).ToArray()
        };
    }

    public override bool IsNoChange(SetChange<TElement> result)
    {
        return result.Removed.Count == 0 && result.Added.Count == 0;
    }

    public override SetChange<TElement> CalculateDelta(SetChange<TElement> previous, SetChange<TElement> current)
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
}

public record SetChange<TElement>
{
    public required IReadOnlyCollection<TElement> Removed { get; init; }
    public required IReadOnlyCollection<TElement> Added { get; init; }
}