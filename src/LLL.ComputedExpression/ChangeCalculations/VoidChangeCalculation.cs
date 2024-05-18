
namespace LLL.ComputedExpression.ChangeCalculations;

public class VoidChangeCalculation<TValue>
    : IChangeCalculation<TValue, VoidChange>
{
    public bool IsIncremental => false;

    public VoidChange GetChange(IComputedValues<TValue> computedValues)
    {
        return default;
    }

    public VoidChange CalculateChange(TValue original, TValue current)
    {
        return default;
    }

    public bool IsNoChange(VoidChange result)
    {
        return false;
    }

    public VoidChange CalculateDelta(VoidChange previous, VoidChange current)
    {
        return default;
    }

    public VoidChange AddDelta(VoidChange value, VoidChange delta)
    {
        return default;
    }
}

public record struct VoidChange();
