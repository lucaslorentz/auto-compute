using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public abstract class ComputedBase
{
    public abstract string ToDebugString();

    public abstract IUnboundChangesProvider ChangesProvider { get; }

    public IEnumerable<IEntityMember> GetDependencies()
    {
        return ChangesProvider.EntityContext.AllAccessedMembers;
    }

    public IEnumerable<ComputedMember> GetComputedDependencies()
    {
        return GetDependencies()
            .OfType<EFCoreEntityMember>()
            .Select(e => e.Property.GetComputed())
            .Where(c => c is not null)
            .Select(c => c!)
            .ToArray();
    }

    public abstract Task<int> Update(DbContext dbContext);
}
