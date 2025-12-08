using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public static class AutoComputeAnnotationAccessors
{
    internal static EFCoreObservedEntityType GetOrCreateObservedEntityType(this IEntityType entityType)
    {
        return entityType.GetOrAddRuntimeAnnotationValue(
            AutoComputeAnnotationNames.ObservedEntityType,
            static (entityType) => new EFCoreObservedEntityType(entityType!),
            entityType);
    }

    internal static EFCoreObservedEntityType? GetObservedEntityType(this IEntityType entityType)
    {
        return entityType.FindRuntimeAnnotationValue(
            AutoComputeAnnotationNames.ObservedEntityType) as EFCoreObservedEntityType;
    }

    internal static EFCoreObservedProperty GetOrCreateObservedProperty(this IProperty property)
    {
        return property.GetOrAddRuntimeAnnotationValue(
            AutoComputeAnnotationNames.ObservedMember,
            static (property) => new EFCoreObservedProperty(property!),
            property);
    }

    internal static EFCoreObservedProperty? GetObservedProperty(this IProperty property)
    {
        return property.FindRuntimeAnnotationValue(
            AutoComputeAnnotationNames.ObservedMember) as EFCoreObservedProperty;
    }

    internal static EFCoreObservedNavigation GetOrCreateObservedNavigation(this INavigationBase navigation)
    {
        return navigation.GetOrAddRuntimeAnnotationValue(
            AutoComputeAnnotationNames.ObservedMember,
            static (navigation) => new EFCoreObservedNavigation(navigation!),
            navigation);
    }

    internal static EFCoreObservedNavigation? GetObservedNavigation(this INavigationBase navigation)
    {
        return navigation.FindRuntimeAnnotationValue(
            AutoComputeAnnotationNames.ObservedMember) as EFCoreObservedNavigation;
    }

    internal static EFCoreObservedMember? GetObservedMember(this IPropertyBase member)
    {
        return member.FindRuntimeAnnotationValue(
            AutoComputeAnnotationNames.ObservedMember) as EFCoreObservedMember;
    }

    internal static ComputedMemberFactory<IProperty>? GetComputedFactory(this IReadOnlyProperty target)
    {
        return target[AutoComputeAnnotationNames.MemberFactory] as ComputedMemberFactory<IProperty>;
    }

    internal static void SetComputedFactory(this IMutableProperty target, ComputedMemberFactory<IProperty> factory)
    {
        target[AutoComputeAnnotationNames.MemberFactory] = factory;
    }

    internal static ComputedMemberFactory<INavigationBase>? GetComputedFactory(this IReadOnlyNavigationBase target)
    {
        return target[AutoComputeAnnotationNames.MemberFactory] as ComputedMemberFactory<INavigationBase>;
    }

    internal static void SetComputedFactory(this IMutableNavigationBase target, ComputedMemberFactory<INavigationBase> factory)
    {
        target[AutoComputeAnnotationNames.MemberFactory] = factory;
    }

    internal static List<ComputedObserverFactory<IEntityType>>? GetObserversFactories(this IReadOnlyEntityType target)
    {
        return target[AutoComputeAnnotationNames.ObserversFactories] as List<ComputedObserverFactory<IEntityType>>;
    }

    internal static void AddObserverFactory(this IMutableEntityType target, ComputedObserverFactory<IEntityType> factory)
    {
        var factories = target.GetObserversFactories();
        if (factories is null)
        {
            factories = [];
            target[AutoComputeAnnotationNames.ObserversFactories] = factories;
        }
        factories.Add(factory);
    }

    public static ComputedMember? GetComputedMember(this IPropertyBase target)
    {
        return target.FindRuntimeAnnotationValue(AutoComputeAnnotationNames.Member) as ComputedMember;
    }

    internal static void SetComputedMember(this IPropertyBase propertyBase, ComputedMember? computedMember)
    {
        propertyBase.SetRuntimeAnnotation(AutoComputeAnnotationNames.Member, computedMember);
    }

    public static IReadOnlyCollection<ComputedObserver>? GetComputedObservers(this IEntityType target)
    {
        return target.FindRuntimeAnnotationValue(AutoComputeAnnotationNames.Observers) as IReadOnlyCollection<ComputedObserver>;
    }

    internal static void SetComputedObservers(this IEntityType target, IReadOnlyCollection<ComputedObserver>? computedObservers)
    {
        target.SetRuntimeAnnotation(AutoComputeAnnotationNames.Observers, computedObservers);
    }

    public static IComputedExpressionAnalyzer GetComputedExpressionAnalyzerOrThrow(this IModel annotatable)
    {
        return annotatable.FindRuntimeAnnotationValue(AutoComputeAnnotationNames.ExpressionAnalyzer) as IComputedExpressionAnalyzer
            ?? throw new Exception($"ExpressionAnalyzer not found in model");
    }

    internal static void SetExpressionAnalyzer(this IModel annotatable, IComputedExpressionAnalyzer analyzer)
    {
        annotatable.SetRuntimeAnnotation(AutoComputeAnnotationNames.ExpressionAnalyzer, analyzer);
    }

    public static IReadOnlyList<ComputedMember> GetAllComputedMembers(this IModel annotatable)
    {
        return annotatable.FindRuntimeAnnotationValue(AutoComputeAnnotationNames.AllMembers) as IReadOnlyList<ComputedMember>
            ?? throw new Exception($"{AutoComputeAnnotationNames.AllMembers} annotation not found in model");
    }

    internal static void SetAllComputedMembers(this IModel annotatable, IReadOnlyList<ComputedMember> computeds)
    {
        annotatable.SetRuntimeAnnotation(AutoComputeAnnotationNames.AllMembers, computeds);
    }

    public static IReadOnlyList<ComputedObserver> GetAllComputedObservers(this IModel annotatable)
    {
        return annotatable.FindRuntimeAnnotationValue(AutoComputeAnnotationNames.AllObservers) as IReadOnlyList<ComputedObserver>
            ?? throw new Exception($"{AutoComputeAnnotationNames.AllObservers} annotation not found in model");
    }

    internal static void SetAllComputedObservers(this IModel annotatable, IReadOnlyList<ComputedObserver> computeds)
    {
        annotatable.SetRuntimeAnnotation(AutoComputeAnnotationNames.AllObservers, computeds);
    }

    internal static LambdaExpression? GetConsistencyFilter(
        this IEntityType entityType)
    {
        return entityType[AutoComputeAnnotationNames.ConsistencyFilter] as LambdaExpression;
    }

    internal static void SetConsistencyFilter(
        this IMutableEntityType entityType,
        LambdaExpression filter)
    {
        entityType[AutoComputeAnnotationNames.ConsistencyFilter] = filter;
    }

    public static LambdaExpression? GetConsistencyCheck(this IReadOnlyPropertyBase target)
    {
        return target[AutoComputeAnnotationNames.ConsistencyCheck] as LambdaExpression;
    }

    internal static void SetConsistencyCheck(this IMutableProperty propertyBase, LambdaExpression check)
    {
        propertyBase[AutoComputeAnnotationNames.ConsistencyCheck] = check;
    }
}
