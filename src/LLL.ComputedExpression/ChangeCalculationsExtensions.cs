using System.Numerics;
using LLL.ComputedExpression.ChangeCalculations;

namespace LLL.ComputedExpression;

public static class ChangeCalculationsExtensions
{
    public static VoidChangeCalculation<TValue> VoidChange<TValue>(
        this ChangeCalculations<TValue> _)
    {
        return new VoidChangeCalculation<TValue>();
    }

    public static CurrentValueChangeCalculation<TValue> CurrentValue<TValue>(
        this ChangeCalculations<TValue> _)
    {
        return new CurrentValueChangeCalculation<TValue>();
    }

    public static ValueChangeCalculation<TValue> ValueChange<TValue>(
        this ChangeCalculations<TValue> calculations)
    {
        return calculations.ValueChange(null);
    }

    public static ValueChangeCalculation<TValue> ValueChange<TValue>(
        this ChangeCalculations<TValue> _,
        IEqualityComparer<TValue>? comparer)
    {
        comparer ??= EqualityComparer<TValue>.Default;
        return new ValueChangeCalculation<TValue>(comparer);
    }

    public static NumberChangeCalculation<TValue> NumberIncremental<TValue>(
        this ChangeCalculations<TValue> _)
        where TValue : INumber<TValue>
    {
        return new NumberChangeCalculation<TValue>();
    }

    public static SetChangeCalculation<TElement> SetIncremental<TElement>(
        this ChangeCalculations<IEnumerable<TElement>> _)
    {
        return _.SetIncremental(null);
    }

    public static SetChangeCalculation<TElement> SetIncremental<TElement>(
        this ChangeCalculations<IEnumerable<TElement>> _,
        IEqualityComparer<TElement>? comparer)
    {
        comparer ??= EqualityComparer<TElement>.Default;
        return new SetChangeCalculation<TElement>(comparer);
    }
}