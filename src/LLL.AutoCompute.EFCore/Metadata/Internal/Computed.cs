using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public abstract class Computed(IUnboundChangesProvider changesProvider)
{
    public abstract string ToDebugString();

    public IUnboundChangesProvider ChangesProvider => changesProvider;

    public IEnumerable<IEntityMember> GetDependencies()
    {
        return changesProvider.EntityContext.AllAccessedMembers;
    }

    public IEnumerable<Computed> GetComputedDependencies()
    {
        return GetDependencies()
            .OfType<EFCoreEntityMember>()
            .SelectMany(e => e.PropertyBase.GetComputeds() ?? [])
            .ToArray();
    }

    public abstract Task<int> Update(DbContext dbContext);
}
