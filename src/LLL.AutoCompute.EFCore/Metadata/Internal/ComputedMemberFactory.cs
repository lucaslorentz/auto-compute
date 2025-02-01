using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Internal;
using LLL.AutoCompute.EFCore.Metadata.Internal.ExpressionVisitors;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public delegate ComputedMember ComputedMemberFactory<in TTarget>(
    IComputedExpressionAnalyzer<IEFCoreComputedInput> analyzer,
    TTarget target);

public class ComputedMemberFactory
{
    public static ComputedMemberFactory<IProperty> CreateComputedPropertyFactory<TEntity, TProperty>(
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
                    property.DeclaringType.ContainingEntityType.GetOrCreateObservedEntityType(),
                    computedExpression,
                    static x => true,
                    changeCalculation);

                if (changesProvider.ObservedMembers.Count == 0)
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

    public static ComputedMemberFactory<INavigationBase> CreateComputedNavigationFactory<TEntity, TProperty>(
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
                    navigation.DeclaringType.ContainingEntityType.GetOrCreateObservedEntityType(),
                    computedExpression,
                    static x => true,
                    changeCalculation);

                if (changesProvider.ObservedMembers.Count == 0)
                    throw new Exception("Computed expression doesn't have observed members");

                var controlledMembers = GetControlledMembers(navigation.TargetEntityType, changesProvider.Expression);

                var computed = new ComputedNavigation<TEntity, TProperty>(
                    navigation,
                    changesProvider,
                    controlledMembers);

                configure?.Invoke(computed);

                return computed;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid computed expression for '{navigation.DeclaringType.ShortName()}.{navigation.Name}': {ex.Message}", ex);
            }
        };
    }


    private static HashSet<IPropertyBase> GetControlledMembers(
        IEntityType entityType,
        LambdaExpression expression)
    {
        var clrMembers = CollectControlledMembersExpressionVisitor.Collect(
            expression, entityType.ClrType);

        var members = new HashSet<IPropertyBase>();

        foreach (var clrMember in clrMembers)
        {
            var member = entityType.FindProperty(clrMember) as IPropertyBase
                ?? entityType.FindNavigation(clrMember) as IPropertyBase
                ?? entityType.FindSkipNavigation(clrMember) as IPropertyBase
                ?? throw new Exception($"Property not found for member {clrMember}");

            members.Add(member);
        }

        return members;
    }
}