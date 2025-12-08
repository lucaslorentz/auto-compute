
namespace LLL.AutoCompute;

public interface IObservedProperty : IObservedMember
{
    Type EntityType { get; }
    Task<ObservedPropertyChanges> GetChangesAsync(ComputedInput input);
}
