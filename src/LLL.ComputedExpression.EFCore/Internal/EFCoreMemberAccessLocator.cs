using System.Collections;
using System.Linq.Expressions;
using LLL.ComputedExpression.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.Computed.EFCore.Internal;

public class EFCoreMemberAccessLocator(IModel model) :
    IAllEntityMemberAccessLocator<IEFCoreComputedInput>
{
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
    ) : IEntityNavigation<IEFCoreComputedInput>
    {
        public string Name => navigation.Name;
        public bool IsCollection => navigation.IsCollection;
        public Type TargetType => navigation.TargetEntityType.ClrType;

        public IEntityNavigation<IEFCoreComputedInput> GetInverse()
        {
            var inverse = navigation.Inverse
                ?? throw new InvalidOperationException($"No inverse for navigation '{navigation.DeclaringType.ShortName()}.{navigation.Name}'");

            return new EntityNavigation(inverse);
        }

        public async Task<IEnumerable<object>> LoadAsync(IEFCoreComputedInput input, IEnumerable<object> targetEntities)
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

        public Expression CreatePreviousValueExpression(
            IEntityMemberAccess<IEntityNavigation> memberAccess,
            Expression inputExpression)
        {
            var oldValueGetter = static (INavigation navigation, object input, object ent) =>
            {
                var dbContext = ((IEFCoreComputedInput)input).DbContext;

                var entityEntry = dbContext.Entry(ent);

                if (entityEntry.State == EntityState.Added)
                    throw new Exception("INVALID!!!");

                return entityEntry.Navigation(navigation).GetOriginalValue();
            };

            return Expression.Convert(
                Expression.Invoke(
                    Expression.Constant(oldValueGetter),
                    Expression.Constant(navigation),
                    inputExpression,
                    memberAccess.FromExpression
                ),
                navigation.ClrType
            );
        }
}

    class EntityProperty(IProperty property) : IEntityProperty
    {
        public string Name => property.Name;
        
        public string ToDebugString()
        {
            return $"{property.Name}";
        }

        public IAffectedEntitiesProvider GetAffectedEntitiesProvider()
        {
            return new PropertyAffectedEntitiesProvider(property);
        }

        public Expression CreatePreviousValueExpression(
            IEntityMemberAccess<IEntityProperty> memberAccess,
            Expression inputExpression)
        {
            var entityEntryExpression =
                Expression.Call(
                    Expression.Property(
                        Expression.Convert(
                            inputExpression,
                            typeof(IEFCoreComputedInput)
                        ),
                        nameof(IEFCoreComputedInput.DbContext)
                    ),
                    nameof(DbContext.Entry),
                    null,
                    memberAccess.FromExpression
                );

            return Expression.Convert(
                Expression.Condition(
                    Expression.Equal(
                        Expression.Property(
                            entityEntryExpression,
                            nameof(EntityEntry.State)
                        ),
                        Expression.Constant(EntityState.Added)
                    ),
                    Expression.Throw(
                        Expression.New(
                            typeof(Exception)
                        ),
                        typeof(object)
                    ),
                    Expression.Property(
                        Expression.Call(
                            entityEntryExpression,
                            nameof(EntityEntry.Property),
                            null,
                            Expression.Constant(property)
                        ),
                        nameof(PropertyEntry.OriginalValue)
                    )
                ),
                property.ClrType
            );
        }
}

    class PropertyAffectedEntitiesProvider(IProperty property)
          : IAffectedEntitiesProvider<IEFCoreComputedInput>
    {
        public string ToDebugString()
        {
            return $"EntitiesWithPropertyChange({property.DeclaringEntityType.ShortName()}, {property.Name})";
        }

        public async Task<IEnumerable<object>> GetAffectedEntitiesAsync(IEFCoreComputedInput input)
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
        : IAffectedEntitiesProvider<IEFCoreComputedInput>
    {
        public string ToDebugString()
        {
            return $"EntitiesWithNavigationChange({navigation.DeclaringEntityType.ShortName()}, {navigation.Name})";
        }

        public async Task<IEnumerable<object>> GetAffectedEntitiesAsync(IEFCoreComputedInput input)
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
                    if (inverseReferenceEntry is not null
                        && (entityEntry.State == EntityState.Added
                        || (entityEntry.State == EntityState.Modified && inverseReferenceEntry.IsModified)
                        || entityEntry.State == EntityState.Deleted))
                    {
                        if (!inverseReferenceEntry.IsLoaded)
                            await inverseReferenceEntry.LoadAsync();

                        if (inverseReferenceEntry.CurrentValue is not null)
                            affectedEntities.Add(inverseReferenceEntry.CurrentValue);

                        var originalValue = inverseReferenceEntry.GetOriginalValue();
                        if (originalValue is not null)
                            affectedEntities.Add(originalValue);
                    }
                }
            }
            return affectedEntities;
        }
    }
}
