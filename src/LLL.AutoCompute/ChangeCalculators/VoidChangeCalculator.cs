
namespace LLL.AutoCompute.ChangeCalculators;

public record class VoidChangeCalculator<TValue>
    : IChangeCalculator<TValue, VoidChange>
{
    public ComputedValueStrategy ValueStrategy => ComputedValueStrategy.NoValue;

    public VoidChange GetChange(IComputedValues<TValue> computedValues)
    {
        return default;
    }

    public bool IsNoChange(VoidChange result)
    {
        return false;
    }

    public VoidChange DeltaChange(VoidChange previous, VoidChange current)
    {
        return default;
    }

    public VoidChange ApplyChange(VoidChange value, VoidChange change)
    {
        return default;
    }
}

public record struct VoidChange();
