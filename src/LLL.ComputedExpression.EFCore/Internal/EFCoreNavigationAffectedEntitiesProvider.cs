using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.ComputedExpression.EFCore.Internal;

public class EFCoreNavigationAffectedEntitiesProvider(INavigation navigation)
    : IAffectedEntitiesProvider<IEFCoreComputedInput>
{
    public virtual string ToDebugString()
    {
        return $"EntitiesWithNavigationChange({navigation.DeclaringEntityType.ShortName()}, {navigation.Name})";
    }

    public virtual async Task<IReadOnlyCollection<object>> GetAffectedEntitiesAsync(IEFCoreComputedInput input)
    {
        var affectedEntities = new HashSet<object>();
        foreach (var entityEntry in input.DbContext.ChangeTracker.Entries())
        {
            if (entityEntry.Metadata == navigation.DeclaringEntityType)
            {
                var navigationEntry = entityEntry.Navigation(navigation);
                if (entityEntry.State == EntityState.Added
                    || (entityEntry.State == EntityState.Modified && navigationEntry.IsModified)
                    || (entityEntry.State == EntityState.Deleted))
                    affectedEntities.Add(entityEntry.Entity);
            }
            else if (navigation.Inverse != null && entityEntry.Metadata == navigation.Inverse.DeclaringEntityType)
            {
                var inverseNavigationEntry = entityEntry.Navigation(navigation.Inverse);
                if (inverseNavigationEntry is not null
                    && (entityEntry.State == EntityState.Added
                    || (entityEntry.State == EntityState.Modified && inverseNavigationEntry.IsModified)
                    || entityEntry.State == EntityState.Deleted))
                {
                    if (!inverseNavigationEntry.IsLoaded)
                        await inverseNavigationEntry.LoadAsync();

                    foreach (var currentEntity in inverseNavigationEntry.GetEntities())
                        affectedEntities.Add(currentEntity);

                    foreach (var originalEntity in inverseNavigationEntry.GetOriginalEntities())
                        affectedEntities.Add(originalEntity);
                }
            }
        }
        return affectedEntities;
    }
}