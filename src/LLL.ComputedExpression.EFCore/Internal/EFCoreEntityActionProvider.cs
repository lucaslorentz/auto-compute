namespace LLL.ComputedExpression.EFCore.Internal;

public class EFCoreEntityActionProvider : IEntityActionProvider<IEFCoreComputedInput>
{
    public EntityAction GetEntityAction(IEFCoreComputedInput input, object entity)
    {
        return input.DbContext.Entry(entity).State switch
        {
            Microsoft.EntityFrameworkCore.EntityState.Added => EntityAction.Create,
            Microsoft.EntityFrameworkCore.EntityState.Deleted => EntityAction.Delete,
            _ => EntityAction.None
        };
    }
}