
namespace LLL.AutoCompute.ChangeCalculations;

public record class VoidChangeCalculation<TValue>
    : IChangeCalculation<TValue, VoidChange>
{
    public bool IsIncremental => false;
    public bool PreLoadEntities => false;

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
