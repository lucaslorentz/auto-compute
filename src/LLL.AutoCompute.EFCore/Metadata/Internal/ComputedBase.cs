using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public abstract class ComputedBase
{
    public abstract string ToDebugString();

    public abstract IUnboundChangesProvider ChangesProvider { get; }

    public IEnumerable<IObservedMember> GetObservedMembers()
    {
        return ChangesProvider.EntityContext.AllAccessedMembers;
    }

    public IEnumerable<ComputedMember> GetComputedDependencies()
    {
        return GetObservedMembers()
            .OfType<EFCoreObservedMember>()
            .Select(e => e.Property.GetComputed())
            .Where(c => c is not null)
            .Select(c => c!)
            .ToArray();
    }

    public abstract Task<UpdateChanges> Update(DbContext dbContext);
}
