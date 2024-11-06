using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public delegate Computed ComputedFactory<in TTarget>(
    IComputedExpressionAnalyzer<IEFCoreComputedInput> analyzer,
    TTarget target);

public class ComputedFactory
{
    public static ComputedFactory<IProperty> CreateComputedPropertyFactory<TEntity, TProperty>(
        Expression<Func<TEntity, TProperty>> computedExpression,
        IChangeCalculation<TProperty, TProperty> changeCalculation)
        where TEntity : class
    {
        return (analyzer, property) =>
        {
            try
            {
                if (property.DeclaringType.ClrType != typeof(TEntity))
                    throw new Exception($"Expected entity type {property.DeclaringType.ClrType} but got {typeof(TEntity)}");

                var changesProvider = analyzer.GetChangesProvider(
                    computedExpression,
                    default,
                    changeCalculation);

                if (!changesProvider.EntityContext.AllAccessedMembers.Any())
                    throw new Exception("Computed expression doesn't have tracked accessed members");

                return new ComputedProperty<TEntity, TProperty>(
                    property,
                    changesProvider);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid computed expression for '{property.DeclaringType.ShortName()}.{property.Name}': {ex.Message}", ex);
            }
        };
    }

    public static ComputedFactory<INavigationBase> CreateComputedNavigationFactory<TEntity, TProperty>(
        Expression<Func<TEntity, TProperty>> computedExpression,
        IChangeCalculation<TProperty, TProperty> changeCalculation)
        where TEntity : class
        where TProperty : class
    {
        return (analyzer, navigation) =>
        {
            try
            {
                if (navigation.DeclaringType.ClrType != typeof(TEntity))
                    throw new Exception($"Expected entity type {navigation.DeclaringType.ClrType} but got {typeof(TEntity)}");

                var changesProvider = analyzer.GetChangesProvider(
                    computedExpression,
                    default,
                    changeCalculation);

                if (!changesProvider.EntityContext.AllAccessedMembers.Any())
                    throw new Exception("Computed expression doesn't have tracked accessed members");

                return new ComputedNavigation<TEntity, TProperty>(
                    navigation,
                    changesProvider);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid computed expression for '{navigation.DeclaringType.ShortName()}.{navigation.Name}': {ex.Message}", ex);
            }
        };

    }
    public static ComputedFactory<INavigationBase> CreateComputedNavigationKeyFactory<TEntity, TProperty, TKey>(
        Expression<Func<TEntity, TKey>> computedExpression,
        IChangeCalculation<TKey, TKey> changeCalculation,
        Expression<Func<TProperty, TKey>> keyExpression)
        where TEntity : class
        where TProperty : class
    {
        return (analyzer, navigation) =>
        {
            try
            {
                if (navigation.DeclaringType.ClrType != typeof(TEntity))
                    throw new Exception($"Expected entity type {navigation.DeclaringType.ClrType} but got {typeof(TEntity)}");

                var changesProvider = analyzer.GetChangesProvider(
                    computedExpression,
                    default,
                    changeCalculation);

                if (!changesProvider.EntityContext.AllAccessedMembers.Any())
                    throw new Exception("Computed expression doesn't have tracked accessed members");

                return new ComputedNavigationKey<TEntity, TProperty, TKey>(
                    navigation,
                    changesProvider,
                    keyExpression);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid computed expression for '{navigation.DeclaringType.ShortName()}.{navigation.Name}': {ex.Message}", ex);
            }
        };
    }

    public static ComputedFactory<IPropertyBase> CreateComputedReactionFactory<TEntity, TProperty, TValue, TChange>(
        Expression<Func<TEntity, TValue>> computedExpression,
        IChangeCalculation<TValue, TChange> changeCalculation,
        Func<TProperty, TChange, TProperty> applyChange)
        where TEntity : class
    {
        return (analyzer, property) =>
        {
            try
            {
                if (property.DeclaringType.ClrType != typeof(TEntity))
                    throw new Exception($"Expected entity type {property.DeclaringType.ClrType} but got {typeof(TEntity)}");

                var changesProvider = analyzer.GetChangesProvider(
                    computedExpression,
                    default,
                    changeCalculation);

                if (!changesProvider.EntityContext.AllAccessedMembers.Any())
                    throw new Exception("Computed expression doesn't have tracked accessed members");

                return new ComputedReaction<TEntity, TProperty, TChange>(
                    property,
                    changesProvider,
                    applyChange);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid computed expression for '{property.DeclaringType.ShortName()}.{property.Name}': {ex.Message}", ex);
            }
        };
    }

    public static ComputedFactory<IEntityType> CreateComputedObserverFactory<TEntity, TValue, TChange>(
        Expression<Func<TEntity, TValue>> computedExpression,
        IChangeCalculation<TValue, TChange> changeCalculation,
        Func<ComputedChangeEventData<TEntity, TChange>, Task> callback)
        where TEntity : class
    {
        return (analyzer, entityType) =>
        {
            try
            {
                var changesProvider = analyzer.GetChangesProvider(
                    computedExpression,
                    default,
                    changeCalculation);

                if (!changesProvider.EntityContext.AllAccessedMembers.Any())
                    throw new Exception("Computed expression doesn't have tracked accessed members");

                return new ComputedObserver<TEntity, TChange>(
                    changesProvider,
                    callback);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid computed expression for reaction in '{entityType.Name}': {ex.Message}", ex);
            }
        };
    }
}