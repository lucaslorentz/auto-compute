using System.Numerics;
using LLL.AutoCompute.ChangeCalculations;

namespace LLL.AutoCompute;

public static class ChangeCalculationsExtensions
{
    public static VoidChangeCalculator<TValue> Void<TValue>(
        this IChangeCalculators<TValue> _)
    {
        return new VoidChangeCalculator<TValue>();
    }

    public static CurrentValueChangeCalculator<TValue> CurrentValue<TValue>(
        this IChangeCalculators<TValue> _)
    {
        return new CurrentValueChangeCalculator<TValue>(false);
    }

    public static CurrentValueChangeCalculator<TValue> CurrentValueIncremental<TValue>(
        this IChangeCalculators<TValue> _)
    {
        return new CurrentValueChangeCalculator<TValue>(true);
    }

    public static ValueChangeCalculator<TValue> ValueChange<TValue>(
        this IChangeCalculators<TValue> _,
        IEqualityComparer<TValue>? comparer = null)
    {
        return new ValueChangeCalculator<TValue>(comparer ?? EqualityComparer<TValue>.Default);
    }

    public static NumberChangeCalculator<TValue> NumberIncremental<TValue>(
        this IChangeCalculators<TValue> _)
        where TValue : INumber<TValue>
    {
        return new NumberChangeCalculator<TValue>(true);
    }

    public static SetChangeCalculator<TElement> SetIncremental<TElement>(
        this IChangeCalculators<IEnumerable<TElement>> _,
        IEqualityComparer<TElement>? comparer = null)
    {
        return new SetChangeCalculator<TElement>(true, comparer ?? EqualityComparer<TElement>.Default);
    }
}