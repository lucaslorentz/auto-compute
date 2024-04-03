using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.ComputedExpression.EFCore.Internal;

public class EFCorePropertyAffectedEntitiesProvider(IProperty property)
      : IAffectedEntitiesProvider<IEFCoreComputedInput>
{
    public virtual string ToDebugString()
    {
        return $"EntitiesWithPropertyChange({property.DeclaringType.ShortName()}, {property.Name})";
    }

    public  virtual async Task<IReadOnlyCollection<object>> GetAffectedEntitiesAsync(IEFCoreComputedInput input)
    {
        var affectedEntities = new HashSet<object>();
        foreach (var entityEntry in input.DbContext.ChangeTracker.Entries())
        {
            if (entityEntry.Metadata == property.DeclaringType)
            {
                var propertyEntry = entityEntry.Property(property);
                if (entityEntry.State == EntityState.Added
                    || (entityEntry.State == EntityState.Modified && propertyEntry.IsModified)
                    || (entityEntry.State == EntityState.Deleted))
                    affectedEntities.Add(entityEntry.Entity);
            }
        }
        return affectedEntities;
    }
}