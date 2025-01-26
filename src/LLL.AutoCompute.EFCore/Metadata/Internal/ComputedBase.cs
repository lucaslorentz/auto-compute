using LLL.AutoCompute.EFCore.Internal;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public abstract class ComputedBase(
    IComputedChangesProvider changesProvider
)
{
    public abstract string ToDebugString();

    public IComputedChangesProvider ChangesProvider => changesProvider;

    public IReadOnlySet<EFCoreObservedMember> ObservedMembers { get; } = changesProvider.ObservedMembers
        .OfType<EFCoreObservedMember>()
        .ToHashSet();

    public IEnumerable<ComputedMember> GetComputedDependencies()
    {
        return ObservedMembers
            .Select(e => e.Property.GetComputedMember())
            .Where(c => c is not null)
            .Select(c => c!)
            .ToArray();
    }

    public abstract Task<EFCoreChangeset> Update(IEFCoreComputedInput input);
}
