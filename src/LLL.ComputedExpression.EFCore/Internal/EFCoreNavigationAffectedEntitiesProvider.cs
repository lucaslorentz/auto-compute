using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.ComputedExpression.EFCore.Internal;

public class EFCoreNavigationAffectedEntitiesProvider<TEntity>(INavigation navigation)
    : IAffectedEntitiesProvider<IEFCoreComputedInput, TEntity>
{
    public virtual string ToDebugString()
    {
        return $"EntitiesWithNavigationChange({navigation.DeclaringEntityType.ShortName()}, {navigation.Name})";
    }

    public virtual async Task<IReadOnlyCollection<TEntity>> GetAffectedEntitiesAsync(IEFCoreComputedInput input)
    {
        var affectedEntities = new HashSet<TEntity>();
        foreach (var entityEntry in input.DbContext.ChangeTracker.Entries())
        {
            if (entityEntry.Metadata == navigation.DeclaringEntityType)
            {
                var navigationEntry = entityEntry.Navigation(navigation);
                if (entityEntry.State == EntityState.Added
                    || (entityEntry.State == EntityState.Modified && navigationEntry.IsModified)
                    || (entityEntry.State == EntityState.Deleted))
                    affectedEntities.Add((TEntity)entityEntry.Entity);
            }
            else if (navigation.Inverse != null && entityEntry.Metadata == navigation.Inverse.DeclaringEntityType)
            {
                var inverseNavigationEntry = entityEntry.Navigation(navigation.Inverse);
                if (inverseNavigationEntry is not null
                    && (entityEntry.State == EntityState.Added
                    || (entityEntry.State == EntityState.Modified && inverseNavigationEntry.IsModified)
                    || entityEntry.State == EntityState.Deleted))
                {
                    if (!inverseNavigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
                        await inverseNavigationEntry.LoadAsync();

                    foreach (var currentEntity in inverseNavigationEntry.GetEntities())
                        affectedEntities.Add((TEntity)currentEntity);

                    foreach (var originalEntity in inverseNavigationEntry.GetOriginalEntities())
                        affectedEntities.Add((TEntity)originalEntity);
                }
            }
        }
        return affectedEntities;
    }
}