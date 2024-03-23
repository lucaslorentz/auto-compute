using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.Computed.EFCore.Internal;

public class EFCoreMemberAccessLocator(IModel model) :
    IAllEntityMemberAccessLocator<EFCoreMemberAccessLocator.IInput>
{
    public interface IInput
    {
        DbContext DbContext { get; }
    }

    IEntityMemberAccess<IEntityNavigation>? IEntityMemberAccessLocator<IEntityNavigation>.GetEntityMemberAccess(Expression node)
    {
        if (node is MemberExpression memberExpression
            && memberExpression.Expression is not null)
        {
            var type = memberExpression.Expression.Type;
            var entityType = model.FindEntityType(type);
            var navigation = entityType?.FindNavigation(memberExpression.Member);
            if (navigation != null)
                return EntityMemberAccess.Create(memberExpression.Expression, new EntityNavigation(navigation));
        }

        return null;
    }

    IEntityMemberAccess<IEntityProperty>? IEntityMemberAccessLocator<IEntityProperty>.GetEntityMemberAccess(Expression node)
    {
        if (node is MemberExpression memberExpression
            && memberExpression.Expression is not null)
        {
            var type = memberExpression.Expression.Type;
            var entityType = model.FindEntityType(type);
            var property = entityType?.FindProperty(memberExpression.Member);
            if (property is not null)
                return EntityMemberAccess.Create(memberExpression.Expression, new EntityProperty(property));
        }

        return null;
    }

    class EntityNavigation(
        INavigation navigation
    ) : IEntityNavigation<IInput>
    {
        public bool IsCollection => navigation.IsCollection;
        public Type TargetType => navigation.TargetEntityType.ClrType;

        public IEntityNavigation<IInput> GetInverse()
        {
            var inverse = navigation.Inverse
                ?? throw new InvalidOperationException($"No inverse for navigation '{navigation.DeclaringType.ShortName()}.{navigation.Name}'");

            return new EntityNavigation(inverse);
        }

        public async Task<IEnumerable<object>> LoadAsync(IInput input, IEnumerable<object> targetEntities)
        {
            var sourceEntities = new HashSet<object>();
            foreach (var targetEntity in targetEntities)
            {
                var navigationEntry = input.DbContext.Entry(targetEntity).Navigation(navigation);
                if (!navigationEntry.IsLoaded)
                {
                    await navigationEntry.LoadAsync();
                }
                if (navigation.IsCollection)
                {
                    if (navigationEntry.CurrentValue is IEnumerable enumerable)
                    {
                        foreach (var sourceEntity in enumerable)
                        {
                            sourceEntities.Add(sourceEntity);
                        }
                    }
                }
                else
                {
                    if (navigationEntry.CurrentValue is not null)
                    {
                        sourceEntities.Add(navigationEntry.CurrentValue);
                    }
                }
            }
            return sourceEntities;
        }

        public string ToDebugString()
        {
            return $"{navigation.Name}";
        }

        public IAffectedEntitiesProvider GetAffectedEntitiesProvider()
        {
            return new NavigationAffectedEntitiesProvider(navigation);
        }

        public Expression CreatePreviousValueExpression(IEntityMemberAccess<IEntityNavigation> expression)
        {
            throw new NotImplementedException();
        }
    }

    class EntityProperty(IProperty property) : IEntityProperty
    {
        public string ToDebugString()
        {
            return $"{property.Name}";
        }

        public IAffectedEntitiesProvider GetAffectedEntitiesProvider()
        {
            return new PropertyAffectedEntitiesProvider(property);
        }

        public Expression CreatePreviousValueExpression(IEntityMemberAccess<IEntityProperty> expression)
        {
            throw new NotImplementedException();
        }
    }

    class PropertyAffectedEntitiesProvider(IProperty property)
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

    class NavigationAffectedEntitiesProvider(INavigation navigation)
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
