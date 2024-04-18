using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.ComputedExpression.EFCore.Internal;

public class EFCoreNavigationAffectedEntitiesProvider<TEntity>(INavigation navigation)
    : IAffectedEntitiesProvider<IEFCoreComputedInput, TEntity>
    where TEntity : class
{
    public virtual string ToDebugString()
    {
        return $"EntitiesWithNavigationChange({navigation.DeclaringEntityType.ShortName()}, {navigation.Name})";
    }

    public virtual async Task<IReadOnlyCollection<TEntity>> GetAffectedEntitiesAsync(IEFCoreComputedInput input)
    {
        return await input.Cache.GetOrCreateAsync((navigation, "AffectedEntities"), async (_) =>
        {
            var affectedEntities = new HashSet<TEntity>();
            foreach (var entityEntry in input.ModifiedEntityEntries[navigation.DeclaringEntityType])
            {
                if (entityEntry.State == EntityState.Added
                    || entityEntry.State == EntityState.Deleted
                    || entityEntry.State == EntityState.Modified)
                {
                    var navigationEntry = entityEntry.Navigation(navigation);
                    if (entityEntry.State == EntityState.Added
                        || entityEntry.State == EntityState.Deleted
                        || navigationEntry.IsModified)
                    {
                        affectedEntities.Add((TEntity)entityEntry.Entity);
                    }
                }
            }
            if (navigation.Inverse is not null)
            {
                foreach (var entityEntry in input.ModifiedEntityEntries[navigation.Inverse.DeclaringEntityType])
                {
                    if (entityEntry.State == EntityState.Added
                        || entityEntry.State == EntityState.Deleted
                        || entityEntry.State == EntityState.Modified)
                    {
                        var inverseNavigationEntry = entityEntry.Navigation(navigation.Inverse);
                        if (entityEntry.State == EntityState.Added
                            || entityEntry.State == EntityState.Deleted
                            || inverseNavigationEntry.IsModified)
                        {
                            if (!inverseNavigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
                                await inverseNavigationEntry.LoadAsync();

                            var entities = inverseNavigationEntry.GetEntities()
                                .Concat(inverseNavigationEntry.GetOriginalEntities());

                            foreach (var entity in entities)
                                affectedEntities.Add((TEntity)entity);
                        }
                    }
                }
            }
            return affectedEntities;
        });
    }
}