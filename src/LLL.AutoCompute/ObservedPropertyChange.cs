namespace LLL.AutoCompute;

public class ObservedPropertyChange(object entity, object? originalValue, object? currentValue)
{
    public object Entity { get; private set; } = entity;
    public object? OriginalValue { get; set; } = originalValue;
    public object? CurrentValue { get; set; } = currentValue;
}