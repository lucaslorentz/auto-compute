using System.Numerics;
using LLL.AutoCompute.ChangeCalculators;

namespace LLL.AutoCompute;

public static class ChangeCalculatorFactoryExtensions
{
    public static VoidChangeCalculator<TValue> Void<TValue>(
        this IChangeCalculatorFactory<TValue> _)
    {
        return new VoidChangeCalculator<TValue>();
    }

    public static CurrentValueChangeCalculator<TValue> CurrentValue<TValue>(
        this IChangeCalculatorFactory<TValue> _)
    {
        return new CurrentValueChangeCalculator<TValue>(false);
    }

    public static CurrentValueChangeCalculator<TValue> CurrentValueIncremental<TValue>(
        this IChangeCalculatorFactory<TValue> _)
    {
        return new CurrentValueChangeCalculator<TValue>(true);
    }

    public static ValueChangeCalculator<TValue> ValueChange<TValue>(
        this IChangeCalculatorFactory<TValue> _,
        IEqualityComparer<TValue>? comparer = null)
    {
        return new ValueChangeCalculator<TValue>(comparer ?? EqualityComparer<TValue>.Default);
    }

    public static NumberChangeCalculator<TValue> NumberIncremental<TValue>(
        this IChangeCalculatorFactory<TValue> _)
        where TValue : INumber<TValue>
    {
        return new NumberChangeCalculator<TValue>(true);
    }

    public static SetChangeCalculator<TElement> SetIncremental<TElement>(
        this IChangeCalculatorFactory<IEnumerable<TElement>> _,
        IEqualityComparer<TElement>? comparer = null)
    {
        return new SetChangeCalculator<TElement>(true, comparer ?? EqualityComparer<TElement>.Default);
    }
}
