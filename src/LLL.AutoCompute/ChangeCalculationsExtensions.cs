using System.Numerics;
using LLL.AutoCompute.ChangeCalculations;

namespace LLL.AutoCompute;

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
        this IChangeCalculations<TValue> _,
        IEqualityComparer<TValue>? comparer = null)
    {
        return new ValueChangeCalculation<TValue>(comparer ?? EqualityComparer<TValue>.Default);
    }

    public static NumberChangeCalculation<TValue> NumberIncremental<TValue>(
        this IChangeCalculations<TValue> _)
        where TValue : INumber<TValue>
    {
        return new NumberChangeCalculation<TValue>(true);
    }

    public static SetChangeCalculation<TElement> SetIncremental<TElement>(
        this IChangeCalculations<IEnumerable<TElement>> _,
        IEqualityComparer<TElement>? comparer = null)
    {
        return new SetChangeCalculation<TElement>(true, comparer ?? EqualityComparer<TElement>.Default);
    }
}