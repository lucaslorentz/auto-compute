using System.Collections;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using LLL.AutoCompute.EFCore.Internal;
using LLL.AutoCompute.Internal.ExpressionVisitors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public abstract class ComputedMember(
    IComputedChangesProvider changesProvider)
    : ComputedBase(changesProvider)
{
    public IEntityType EntityType => Property.DeclaringType.ContainingEntityType;
    public abstract IPropertyBase Property { get; }
    public abstract Task<ComputedMemberConsistency> CheckConsistencyAsync(DbContext dbContext, DateTime since);
    public abstract IQueryable QueryInconsistentEntities(DbContext dbContext, DateTime since);
    public abstract LambdaExpression CreateIsEntityInconsistentLambda();
    public abstract Task FixAsync(object entity, DbContext dbContext);

    public override string ToDebugString()
    {
        return $"{Property.DeclaringType.Name}.{Property.Name}";
    }

    public abstract Task<EFCoreChangeset> Update(EFCoreComputedInput input);

    protected static void MaybeUpdateProperty(PropertyEntry propertyEntry, object? newValue, EFCoreChangeset? updateChanges)
    {
        var valueComparer = propertyEntry.Metadata.GetValueComparer();
        if (valueComparer.Equals(propertyEntry.CurrentValue, newValue))
            return;

        updateChanges?.AddPropertyChange(propertyEntry.Metadata, propertyEntry.EntityEntry.Entity);

        propertyEntry.CurrentValue = newValue;
    }

    protected static async Task MaybeUpdateNavigation(
        NavigationEntry navigationEntry,
        object? newValue,
        EFCoreChangeset? updateChanges,
        IReadOnlySet<IPropertyBase> updateMembers,
        Delegate? reuseKeySelector)
    {
        var navigation = navigationEntry.Metadata;
        var dbContext = navigationEntry.EntityEntry.Context;
        var entity = navigationEntry.EntityEntry.Entity;
        var itemsToRemove = navigationEntry.GetOriginalEntities().ToHashSet();
        var itemsToAdd = new HashSet<object>();
        var newItems = navigation.IsCollection
            ? (newValue is IEnumerable enumerable ? enumerable : Array.Empty<object>())
            : (newValue is not null ? [newValue] : Array.Empty<object>());

        foreach (var newItem in newItems)
        {
            var existingItem = reuseKeySelector is not null
                ? FindEntityToReuse(itemsToRemove, newItem, reuseKeySelector)
                : null;

            if (existingItem is null)
            {
                if (dbContext.Entry(newItem).State == EntityState.Detached)
                    dbContext.Add(newItem);

                itemsToAdd.Add(newItem);

                if (updateChanges is not null)
                {
                    var entry = dbContext.Entry(newItem);
                    foreach (var member in updateMembers)
                    {
                        var observedMember = member.GetObservedMember();
                        if (observedMember is null)
                            continue;

                        await observedMember.CollectChangesAsync(entry, updateChanges);
                    }
                }
            }
            else
            {
                itemsToRemove.Remove(existingItem);

                foreach (var memberToUpdate in updateMembers)
                {
                    var existingEntityEntry = dbContext.Entry(existingItem);
                    var existingMemberEntry = existingEntityEntry.Member(memberToUpdate);
                    var newMemberValue = memberToUpdate.GetGetter().GetClrValueUsingContainingEntity(newItem);
                    switch (existingMemberEntry)
                    {
                        case PropertyEntry existingPropertyEntry:
                            MaybeUpdateProperty(
                                existingPropertyEntry,
                                newMemberValue,
                                updateChanges);
                            break;
                        case NavigationEntry existingNavigationEntry:
                            await MaybeUpdateNavigation(
                                existingNavigationEntry,
                                newMemberValue,
                                updateChanges,
                                ImmutableHashSet<IPropertyBase>.Empty,
                                null);
                            break;
                        default:
                            throw new NotSupportedException($"Controlled member {memberToUpdate} is not supported");
                    }
                }
            }
        }

        var collectionAccessor = navigation.GetCollectionAccessor();
        foreach (var entityToRemove in itemsToRemove)
        {
            if (collectionAccessor is not null)
                collectionAccessor.Remove(entity, entityToRemove);
            else
                navigationEntry.CurrentValue = null;

            if (updateChanges is not null)
            {
                updateChanges.RegisterNavigationRemoved(navigation, entity, entityToRemove);

                if (navigation.Inverse is not null)
                    updateChanges.RegisterNavigationRemoved(navigation.Inverse, entityToRemove, entity);
            }
        }

        foreach (var entityToAdd in itemsToAdd)
        {
            if (collectionAccessor is not null)
                collectionAccessor.Add(entity, entityToAdd, false);
            else
                navigationEntry.CurrentValue = entityToAdd;

            if (updateChanges is not null)
            {
                updateChanges.RegisterNavigationAdded(navigation, entity, entityToAdd);

                if (navigation.Inverse is not null)
                    updateChanges.RegisterNavigationAdded(navigation.Inverse, entityToAdd, entity);
            }
        }
    }

    private static object? FindEntityToReuse(
        IEnumerable<object> availableEntities,
        object newEntity,
        Delegate reuseKeySelector)
    {
        if (reuseKeySelector is null)
            return null;

        var reuseKey = reuseKeySelector.DynamicInvoke(newEntity);
        return availableEntities.FirstOrDefault(x => Equals(reuseKeySelector.DynamicInvoke(x), reuseKey));
    }
}

public abstract class ComputedMember<TEntity, TMember>(
    IComputedChangesProvider changesProvider)
    : ComputedMember(changesProvider)
    where TEntity : class
{
    public override async Task<ComputedMemberConsistency> CheckConsistencyAsync(DbContext dbContext, DateTime since)
    {
        var result = await dbContext.CreateConsistencyQuery<TEntity>(EntityType, since)
            .GroupBy(CreateIsEntityInconsistentLambda())
            .ToDictionaryAsync(x => x.Key, x => x.Count());

        if (!result.TryGetValue(false, out var consistent))
            consistent = 0;

        if (!result.TryGetValue(true, out var inconsistent))
            inconsistent = 0;

        return new ComputedMemberConsistency(consistent, inconsistent);
    }

    public override IQueryable<TEntity> QueryInconsistentEntities(DbContext dbContext, DateTime since)
    {
        return dbContext.CreateConsistencyQuery<TEntity>(EntityType, since)
            .Where(CreateIsEntityInconsistentLambda());
    }

    public override Expression<Func<TEntity, bool>> CreateIsEntityInconsistentLambda()
    {
        var entParameter = ChangesProvider.Expression.Parameters.First();

        var computedValue = new RemoveChangeComputedTrackingVisitor().Visit(
            ChangesProvider.Expression.Body
        );

        var storedValue = CreateEFPropertyExpression(entParameter, Property);

        return Expression.Lambda<Func<TEntity, bool>>(
            CreateIsValueInconsistentExpression(computedValue, storedValue),
            entParameter
        );
    }

    protected abstract Expression CreateIsValueInconsistentExpression(Expression computedValue, Expression storedValue);

    private static readonly MethodInfo _efPropertyMethod = ((Func<object, string, object>)EF.Property<object>)
        .Method.GetGenericMethodDefinition();

    protected static Expression CreateEFPropertyExpression(
        Expression expression,
        IPropertyBase property)
    {
        if (property.PropertyInfo is not null)
        {
            return Expression.Property(
                expression,
                property.PropertyInfo
            );
        }
        else if (property.FieldInfo is not null)
        {
            return Expression.Field(
                expression,
                property.FieldInfo
            );
        }
        else if (property.IsShadowProperty())
        {
            return Expression.Call(
                null,
                _efPropertyMethod.MakeGenericMethod(property.ClrType),
                expression,
                Expression.Constant(property.Name)
            );
        }
        else
        {
            throw new Exception("Unsupported property access");
        }
    }
}
