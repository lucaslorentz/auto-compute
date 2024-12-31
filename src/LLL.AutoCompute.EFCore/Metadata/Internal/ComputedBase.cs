using LLL.AutoCompute.EFCore.Internal;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public abstract class ComputedBase
{
    public abstract string ToDebugString();

    public abstract IUnboundChangesProvider ChangesProvider { get; }

    public IEnumerable<EFCoreObservedMember> GetObservedMembers()
    {
        return ChangesProvider.EntityContext.GetAllObservedMembers().OfType<EFCoreObservedMember>();
    }

    public IEnumerable<ComputedMember> GetComputedDependencies()
    {
        return GetObservedMembers()
            .Select(e => e.Property.GetComputed())
            .Where(c => c is not null)
            .Select(c => c!)
            .ToArray();
    }

    public abstract Task<EFCoreChangeset> Update(IEFCoreComputedInput input);
}
