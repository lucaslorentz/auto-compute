using LLL.AutoCompute.EFCore.Internal;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public abstract class ComputedBase(
    IComputedChangesProvider changesProvider
)
{
    public abstract string ToDebugString();

    public IComputedChangesProvider ChangesProvider => changesProvider;

    public IReadOnlySet<EFCoreObservedMember> ObservedMembers { get; } = changesProvider.EntityContext
        .GetAllObservedMembers()
        .OfType<EFCoreObservedMember>()
        .ToHashSet();

    public IReadOnlySet<EFCoreObservedEntityType> ObservedEntityTypes { get; } = changesProvider.EntityContext
        .GetAllObservedEntityTypes()
        .OfType<EFCoreObservedEntityType>()
        .ToHashSet();

    public IEnumerable<ComputedMember> GetComputedDependencies()
    {
        return ObservedMembers
            .Select(e => e.Member.GetComputedMember())
            .Where(c => c is not null)
            .Select(c => c!)
            .ToArray();
    }
}
