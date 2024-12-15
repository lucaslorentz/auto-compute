using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public static class ComputedAnnotationAccessors
{
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

    internal static ComputedFactory<IProperty>? GetComputedFactory(this IReadOnlyProperty target)
    {
        return target[ComputedAnnotationNames.ComputedFactory] as ComputedFactory<IProperty>;
    }

    internal static void SetComputedFactory(this IMutableProperty target, ComputedFactory<IProperty> factory)
    {
        target[ComputedAnnotationNames.ComputedFactory] = factory;
    }

    internal static ComputedFactory<INavigationBase>? GetComputedFactory(this IReadOnlyNavigationBase target)
    {
        return target[ComputedAnnotationNames.ComputedFactory] as ComputedFactory<INavigationBase>;
    }

    internal static void SetComputedFactory(this IMutableNavigationBase target, ComputedFactory<INavigationBase> factory)
    {
        target[ComputedAnnotationNames.ComputedFactory] = factory;
    }

    internal static List<ObserverFactory<IEntityType>>? GetObserversFactories(this IReadOnlyEntityType target)
    {
        return target[ComputedAnnotationNames.ObserversFactories] as List<ObserverFactory<IEntityType>>;
    }

    internal static void AddObserverFactory(this IMutableEntityType target, ObserverFactory<IEntityType> factory)
    {
        var factories = target.GetObserversFactories();
        if (factories is null)
        {
            factories = [];
            target[ComputedAnnotationNames.ObserversFactories] = factories;
        }
        factories.Add(factory);
    }

    public static ComputedMember? GetComputed(this IPropertyBase target)
    {
        return target.FindRuntimeAnnotationValue(ComputedAnnotationNames.Computed) as ComputedMember;
    }

    internal static void SetComputed(this IPropertyBase propertyBase, ComputedMember? computeds)
    {
        propertyBase.SetRuntimeAnnotation(ComputedAnnotationNames.Computed, computeds);
    }

    public static IReadOnlyList<ComputedObserver>? GetObservers(this IEntityType target)
    {
        return target.FindRuntimeAnnotationValue(ComputedAnnotationNames.Observers) as IReadOnlyList<ComputedObserver>;
    }

    internal static void SetObservers(this IEntityType target, IReadOnlyList<ComputedObserver>? observers)
    {
        target.SetRuntimeAnnotation(ComputedAnnotationNames.Observers, observers);
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

    internal static IReadOnlyList<ComputedBase> GetSortedComputedsOrThrow(this IModel annotatable)
    {
        return annotatable.FindRuntimeAnnotationValue(ComputedAnnotationNames.SortedComputeds) as IReadOnlyList<ComputedBase>
            ?? throw new Exception($"SortedComputeds not found in model");
    }

    internal static void SetSortedComputeds(this IModel annotatable, IReadOnlyList<ComputedBase> computeds)
    {
        annotatable.SetRuntimeAnnotation(ComputedAnnotationNames.SortedComputeds, computeds);
    }
}
