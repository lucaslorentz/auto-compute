using Microsoft.EntityFrameworkCore;

namespace LLL.Computed.EFCore.Internal;

public class EFCoreAffectedEntitiesInput(DbContext dbContext) :
    EFCoreEntityChangeTracker.IInput,
    EFCoreEntityNavigationProvider.IInput
{
    public DbContext DbContext => dbContext;
}
