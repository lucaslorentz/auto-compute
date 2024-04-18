using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.ComputedExpression.EFCore.Internal;

public class EFCorePropertyAffectedEntitiesProvider<TEntity>(IProperty property)
      : IAffectedEntitiesProvider<IEFCoreComputedInput, TEntity>
      where TEntity : class
{
    public virtual string ToDebugString()
    {
        return $"EntitiesWithPropertyChange({property.DeclaringType.ShortName()}, {property.Name})";
    }

    public virtual async Task<IReadOnlyCollection<TEntity>> GetAffectedEntitiesAsync(IEFCoreComputedInput input)
    {
        return await input.Cache.GetOrCreateAsync((property, "AffectedEntities"), async (_) =>
        {
            var affectedEntities = new HashSet<TEntity>();
            foreach (var entityEntry in input.ModifiedEntityEntries[property.DeclaringType])
            {
                if (entityEntry.State == EntityState.Added
                    || entityEntry.State == EntityState.Deleted
                    || entityEntry.State == EntityState.Modified)
                {
                    var propertyEntry = entityEntry.Property(property);
                    if (entityEntry.State == EntityState.Added
                        || entityEntry.State == EntityState.Deleted
                        || propertyEntry.IsModified)
                    {
                        affectedEntities.Add((TEntity)entityEntry.Entity);
                    }
                }
            }
            return affectedEntities;
        });
    }
}