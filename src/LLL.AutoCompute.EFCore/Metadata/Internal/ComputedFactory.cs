using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public delegate ComputedMember ComputedFactory<in TTarget>(
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

                var changesProvider = analyzer.CreateChangesProvider(
                    computedExpression,
                    static x => true,
                    changeCalculation);

                if (!changesProvider.EntityContext.GetAllObservedMembers().Any())
                    throw new Exception("Computed expression doesn't have observed members");

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
        IChangeCalculation<TProperty, TProperty> changeCalculation,
        Action<IComputedNavigationBuilder<TEntity, TProperty>>? configure)
        where TEntity : class
        where TProperty : class
    {
        return (analyzer, navigation) =>
        {
            try
            {
                if (navigation.DeclaringType.ClrType != typeof(TEntity))
                    throw new Exception($"Expected entity type {navigation.DeclaringType.ClrType} but got {typeof(TEntity)}");

                var changesProvider = analyzer.CreateChangesProvider(
                    computedExpression,
                    static x => true,
                    changeCalculation);

                if (!changesProvider.EntityContext.GetAllObservedMembers().Any())
                    throw new Exception("Computed expression doesn't have observed members");

                var computed = new ComputedNavigation<TEntity, TProperty>(
                    navigation,
                    changesProvider);

                configure?.Invoke(computed);

                return computed;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid computed expression for '{navigation.DeclaringType.ShortName()}.{navigation.Name}': {ex.Message}", ex);
            }
        };
    }
}