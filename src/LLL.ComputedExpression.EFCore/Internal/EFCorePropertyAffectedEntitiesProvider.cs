using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.ComputedExpression.EFCore.Internal;

public class EFCorePropertyAffectedEntitiesProvider<TEntity>(IProperty property)
      : IAffectedEntitiesProvider<IEFCoreComputedInput, TEntity>
{
    public virtual string ToDebugString()
    {
        return $"EntitiesWithPropertyChange({property.DeclaringType.ShortName()}, {property.Name})";
    }

    public  virtual async Task<IReadOnlyCollection<TEntity>> GetAffectedEntitiesAsync(IEFCoreComputedInput input)
    {
        var affectedEntities = new HashSet<TEntity>();
        foreach (var entityEntry in input.DbContext.ChangeTracker.Entries())
        {
            if (entityEntry.Metadata == property.DeclaringType)
            {
                var propertyEntry = entityEntry.Property(property);
                if (entityEntry.State == EntityState.Added
                    || (entityEntry.State == EntityState.Modified && propertyEntry.IsModified)
                    || (entityEntry.State == EntityState.Deleted))
                    affectedEntities.Add((TEntity)entityEntry.Entity);
            }
        }
        return affectedEntities;
    }
}