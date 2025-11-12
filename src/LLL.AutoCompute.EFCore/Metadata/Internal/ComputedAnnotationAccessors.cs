using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public static class ComputedAnnotationAccessors
{
    internal static EFCoreObservedEntityType GetOrCreateObservedEntityType(this IEntityType entityType)
    {
        return entityType.GetOrAddRuntimeAnnotationValue(
            ComputedAnnotationNames.ObservedEntityType,
            static (entityType) => new EFCoreObservedEntityType(entityType!),
            entityType);
    }

    internal static EFCoreObservedEntityType? GetObservedEntityType(this IEntityType entityType)
    {
        return entityType.FindRuntimeAnnotationValue(
            ComputedAnnotationNames.ObservedEntityType) as EFCoreObservedEntityType;
    }

    internal static EFCoreObservedProperty GetOrCreateObservedProperty(this IProperty property)
    {
        return property.GetOrAddRuntimeAnnotationValue(
            ComputedAnnotationNames.ObservedMember,
            static (property) => new EFCoreObservedProperty(property!),
            property);
    }

    internal static EFCoreObservedProperty? GetObservedProperty(this IProperty property)
    {
        return property.FindRuntimeAnnotationValue(
            ComputedAnnotationNames.ObservedMember) as EFCoreObservedProperty;
    }

    internal static EFCoreObservedNavigation GetOrCreateObservedNavigation(this INavigationBase navigation)
    {
        return navigation.GetOrAddRuntimeAnnotationValue(
            ComputedAnnotationNames.ObservedMember,
            static (navigation) => new EFCoreObservedNavigation(navigation!),
            navigation);
    }

    internal static EFCoreObservedNavigation? GetObservedNavigation(this INavigationBase navigation)
    {
        return navigation.FindRuntimeAnnotationValue(
            ComputedAnnotationNames.ObservedMember) as EFCoreObservedNavigation;
    }

    internal static EFCoreObservedMember? GetObservedMember(this IPropertyBase member)
    {
        return member.FindRuntimeAnnotationValue(
            ComputedAnnotationNames.ObservedMember) as EFCoreObservedMember;
    }

    internal static ComputedMemberFactory<IProperty>? GetComputedFactory(this IReadOnlyProperty target)
    {
        return target[ComputedAnnotationNames.MemberFactory] as ComputedMemberFactory<IProperty>;
    }

    internal static void SetComputedFactory(this IMutableProperty target, ComputedMemberFactory<IProperty> factory)
    {
        target[ComputedAnnotationNames.MemberFactory] = factory;
    }

    internal static ComputedMemberFactory<INavigationBase>? GetComputedFactory(this IReadOnlyNavigationBase target)
    {
        return target[ComputedAnnotationNames.MemberFactory] as ComputedMemberFactory<INavigationBase>;
    }

    internal static void SetComputedFactory(this IMutableNavigationBase target, ComputedMemberFactory<INavigationBase> factory)
    {
        target[ComputedAnnotationNames.MemberFactory] = factory;
    }

    internal static List<ComputedObserverFactory<IEntityType>>? GetObserversFactories(this IReadOnlyEntityType target)
    {
        return target[ComputedAnnotationNames.ObserversFactories] as List<ComputedObserverFactory<IEntityType>>;
    }

    internal static void AddObserverFactory(this IMutableEntityType target, ComputedObserverFactory<IEntityType> factory)
    {
        var factories = target.GetObserversFactories();
        if (factories is null)
        {
            factories = [];
            target[ComputedAnnotationNames.ObserversFactories] = factories;
        }
        factories.Add(factory);
    }

    public static ComputedMember? GetComputedMember(this IPropertyBase target)
    {
        return target.FindRuntimeAnnotationValue(ComputedAnnotationNames.Member) as ComputedMember;
    }

    internal static void SetComputedMember(this IPropertyBase propertyBase, ComputedMember? computedMember)
    {
        propertyBase.SetRuntimeAnnotation(ComputedAnnotationNames.Member, computedMember);
    }

    public static IReadOnlyCollection<ComputedObserver>? GetComputedObservers(this IEntityType target)
    {
        return target.FindRuntimeAnnotationValue(ComputedAnnotationNames.Observers) as IReadOnlyCollection<ComputedObserver>;
    }

    internal static void SetComputedObservers(this IEntityType target, IReadOnlyCollection<ComputedObserver>? computedObservers)
    {
        target.SetRuntimeAnnotation(ComputedAnnotationNames.Observers, computedObservers);
    }

    public static IComputedExpressionAnalyzer<IEFCoreComputedInput> GetComputedExpressionAnalyzerOrThrow(this IModel annotatable)
    {
        return annotatable.FindRuntimeAnnotationValue(ComputedAnnotationNames.ExpressionAnalyzer) as IComputedExpressionAnalyzer<IEFCoreComputedInput>
            ?? throw new Exception($"ExpressionAnalyzer not found in model");
    }

    internal static void SetExpressionAnalyzer(this IModel annotatable, IComputedExpressionAnalyzer<IEFCoreComputedInput> analyzer)
    {
        annotatable.SetRuntimeAnnotation(ComputedAnnotationNames.ExpressionAnalyzer, analyzer);
    }

    public static IReadOnlyList<ComputedMember> GetAllComputedMembers(this IModel annotatable)
    {
        return annotatable.FindRuntimeAnnotationValue(ComputedAnnotationNames.AllMembers) as IReadOnlyList<ComputedMember>
            ?? throw new Exception($"{ComputedAnnotationNames.AllMembers} annotation not found in model");
    }

    internal static void SetAllComputedMembers(this IModel annotatable, IReadOnlyList<ComputedMember> computeds)
    {
        annotatable.SetRuntimeAnnotation(ComputedAnnotationNames.AllMembers, computeds);
    }

    public static IReadOnlyList<ComputedObserver> GetAllComputedObservers(this IModel annotatable)
    {
        return annotatable.FindRuntimeAnnotationValue(ComputedAnnotationNames.AllObservers) as IReadOnlyList<ComputedObserver>
            ?? throw new Exception($"{ComputedAnnotationNames.AllObservers} annotation not found in model");
    }

    internal static void SetAllComputedObservers(this IModel annotatable, IReadOnlyList<ComputedObserver> computeds)
    {
        annotatable.SetRuntimeAnnotation(ComputedAnnotationNames.AllObservers, computeds);
    }

    internal static LambdaExpression? GetConsistencyFilter(
        this IEntityType entityType)
    {
        return entityType[ComputedAnnotationNames.ConsistencyFilter] as LambdaExpression;
    }

    internal static void SetConsistencyFilter(
        this IMutableEntityType entityType,
        LambdaExpression filter)
    {
        entityType[ComputedAnnotationNames.ConsistencyFilter] = filter;
    }

    public static LambdaExpression? GetConsistencyCheck(this IReadOnlyPropertyBase target)
    {
        return target[ComputedAnnotationNames.ConsistencyCheck] as LambdaExpression;
    }

    internal static void SetConsistencyCheck(this IMutableProperty propertyBase, LambdaExpression check)
    {
        propertyBase[ComputedAnnotationNames.ConsistencyCheck] = check;
    }
}
