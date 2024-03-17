using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace L3.Computed.EFCore.Internal;

public class EFCoreEntityChangeTracker(IModel model)
    : IEntityChangeTracker<EFCoreEntityChangeTracker.IInput>
{
    public interface IInput
    {
        DbContext DbContext { get; }
    }

    public void TrackChanges(
        Expression node,
        IComputedExpressionAnalysis analysis)
    {
        if (node is MemberExpression memberExpression
            && memberExpression.Expression is not null)
        {
            var entityType = model.FindEntityType(memberExpression.Expression.Type);

            var navigation = entityType?.FindNavigation(memberExpression.Member);
            if (navigation is not null)
            {
                var entityContext = analysis.ResolveEntityContext(memberExpression.Expression, EntityContextKeys.None);
                if (entityContext.IsTrackingChanges)
                {
                    entityContext.AddAffectedEntitiesProvider(new NavigationAffectedEntitiesProvider(navigation));
                }
            }

            var property = entityType?.FindProperty(memberExpression.Member);
            if (property is not null)
            {
                var entityContext = analysis.ResolveEntityContext(memberExpression.Expression, EntityContextKeys.None);
                if (entityContext.IsTrackingChanges)
                {
                    entityContext.AddAffectedEntitiesProvider(new PropertyAffectedEntitiesProvider(property));
                }
            }
        }
    }

    public class PropertyAffectedEntitiesProvider(IProperty property)
        : IAffectedEntitiesProvider<IInput>
    {
        public string ToDebugString()
        {
            return $"EntitiesWithPropertyChange({property.DeclaringEntityType.ShortName()}, {property.Name})";
        }

        public async Task<IEnumerable<object>> GetAffectedEntitiesAsync(IInput input)
        {
            var affectedEntities = new HashSet<object>();
            foreach (var entityEntry in input.DbContext.ChangeTracker.Entries())
            {
                if (entityEntry.Metadata == property.DeclaringEntityType)
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

    public class NavigationAffectedEntitiesProvider(INavigation navigation)
        : IAffectedEntitiesProvider<IInput>
    {
        public string ToDebugString()
        {
            return $"EntitiesWithNavigationChange({navigation.DeclaringEntityType.ShortName()}, {navigation.Name})";
        }

        public async Task<IEnumerable<object>> GetAffectedEntitiesAsync(IInput input)
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
                    var inverseReferenceEntry = entityEntry.Reference(navigation.Inverse);
                    if (inverseReferenceEntry is not null && inverseReferenceEntry.IsModified)
                    {
                        if (!inverseReferenceEntry.IsLoaded)
                            await inverseReferenceEntry.LoadAsync();

                        if (inverseReferenceEntry.CurrentValue is not null)
                        {
                            affectedEntities.Add(inverseReferenceEntry.CurrentValue);
                        }

                        var oldKeyValues = navigation.Inverse.ForeignKey.Properties
                            .Select(p => entityEntry.OriginalValues[p])
                            .ToArray();

                        var oldValue = input.DbContext.Find(navigation.Inverse.TargetEntityType.ClrType, oldKeyValues);

                        if (oldValue is not null)
                        {
                            affectedEntities.Add(oldValue);
                        }
                    }
                }
            }
            return affectedEntities;
        }
    }
}
