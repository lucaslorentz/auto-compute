using Microsoft.EntityFrameworkCore;

namespace L3.Computed.EFCore.Internal;

public class EFCoreAffectedEntitiesInput(DbContext dbContext) :
    EFCoreEntityChangeTracker.IInput,
    EFCoreEntityNavigationProvider.IInput
{
    public DbContext DbContext => dbContext;
}
