namespace LLL.AutoCompute;

public interface IObservedNavigation : IObservedMember
{
    IObservedEntityType SourceEntityType { get; }
    IObservedEntityType TargetEntityType { get; }
    bool IsCollection { get; }
    IObservedNavigation GetInverse();
    Task<IReadOnlyDictionary<object, IReadOnlyCollection<object>>> LoadCurrentAsync(ComputedInput input, IReadOnlyCollection<object> fromEntities);
    Task<IReadOnlyDictionary<object, IReadOnlyCollection<object>>> LoadOriginalAsync(ComputedInput input, IReadOnlyCollection<object> fromEntities);
    Task<ObservedNavigationChanges> GetChangesAsync(ComputedInput input);
}
