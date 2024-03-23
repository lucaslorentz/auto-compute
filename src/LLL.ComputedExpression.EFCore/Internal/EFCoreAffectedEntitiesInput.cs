using Microsoft.EntityFrameworkCore;

namespace LLL.Computed.EFCore.Internal;

public class EFCoreAffectedEntitiesInput(DbContext dbContext) :
    EFCoreMemberAccessLocator.IInput
{
    public DbContext DbContext => dbContext;
}
