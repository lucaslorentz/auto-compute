namespace LLL.AutoCompute;

public interface IObservedEntityType
{
    string Name { get; }
    ObservedEntityState GetEntityState(ComputedInput input, object entity);
    bool IsInstanceOfType(object obj);
}
