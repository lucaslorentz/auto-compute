using System.Numerics;
using LLL.ComputedExpression.ChangeCalculations;

namespace LLL.ComputedExpression;

public static class ChangeCalculationsExtensions
{
    public static VoidChangeCalculation<TValue> Void<TValue>(
        this IChangeCalculations<TValue> _)
    {
        return new VoidChangeCalculation<TValue>();
    }

    public static CurrentValueChangeCalculation<TValue> CurrentValue<TValue>(
        this IChangeCalculations<TValue> _)
    {
        return new CurrentValueChangeCalculation<TValue>(false);
    }

    public static CurrentValueChangeCalculation<TValue> CurrentValueIncremental<TValue>(
        this IChangeCalculations<TValue> _)
    {
        return new CurrentValueChangeCalculation<TValue>(true);
    }

    public static ValueChangeCalculation<TValue> ValueChange<TValue>(
        this IChangeCalculations<TValue> calculations)
    {
        return calculations.ValueChange(null);
    }

    public static ValueChangeCalculation<TValue> ValueChange<TValue>(
        this IChangeCalculations<TValue> _,
        IEqualityComparer<TValue>? comparer)
    {
        comparer ??= EqualityComparer<TValue>.Default;
        return new ValueChangeCalculation<TValue>(false, comparer);
    }

    public static NumberChangeCalculation<TValue> NumberIncremental<TValue>(
        this IChangeCalculations<TValue> _)
        where TValue : INumber<TValue>
    {
        return new NumberChangeCalculation<TValue>(true);
    }

    public static SetChangeCalculation<TElement> SetIncremental<TElement>(
        this IChangeCalculations<IEnumerable<TElement>> _)
    {
        return _.SetIncremental(null);
    }

    public static SetChangeCalculation<TElement> SetIncremental<TElement>(
        this IChangeCalculations<IEnumerable<TElement>> _,
        IEqualityComparer<TElement>? comparer)
    {
        comparer ??= EqualityComparer<TElement>.Default;
        return new SetChangeCalculation<TElement>(true, comparer);
    }
}