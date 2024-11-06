using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public static class ComputedAnnotationAccessors
{
    internal static List<ComputedFactory<IEntityType>>? GetComputedFactories(
        this IEntityType target)
    {
        return target.GetComputedFactoriesInternal<IEntityType>();
    }

    internal static List<ComputedFactory<IProperty>>? GetComputedFactories(
        this IProperty target)
    {
        return target.GetComputedFactoriesInternal<IProperty>();
    }

    internal static List<ComputedFactory<INavigationBase>>? GetComputedFactories(
        this INavigationBase target)
    {
        return target.GetComputedFactoriesInternal<INavigationBase>();
    }

    internal static void AddComputedFactory(
        this IMutableProperty metadata,
        ComputedFactory<IProperty> factory)
    {
        metadata.AddComputedFactoryInternal(factory);
    }

    internal static void AddComputedFactory(
        this IMutableNavigationBase metadata,
        ComputedFactory<INavigationBase> factory)
    {
        metadata.AddComputedFactoryInternal(factory);
    }

    internal static void AddComputedFactory(
        this IMutableEntityType metadata,
        ComputedFactory<IEntityType> factory)
    {
        metadata.AddComputedFactoryInternal(factory);
    }

    private static List<ComputedFactory<TTarget>>? GetComputedFactoriesInternal<TTarget>(
        this IReadOnlyAnnotatable target)
    {
        return target[ComputedAnnotationNames.Factories] as List<ComputedFactory<TTarget>>;
    }

    private static void AddComputedFactoryInternal<TTarget>(
        this IMutableAnnotatable mutableTarget,
        ComputedFactory<TTarget> factory)
    {
        var factories = mutableTarget.GetComputedFactoriesInternal<TTarget>();
        if (factories is null)
        {
            factories = [];
            mutableTarget[ComputedAnnotationNames.Factories] = factories;
        }
        factories.Add(factory);
    }

    public static IReadOnlyList<Computed>? GetComputeds(this IAnnotatable annotatable)
    {
        return annotatable.FindRuntimeAnnotationValue(ComputedAnnotationNames.Computeds) as IReadOnlyList<Computed>;
    }

    internal static void SetComputeds(
        this IAnnotatable annotatable,
        IReadOnlyList<Computed>? computeds)
    {
        annotatable.SetRuntimeAnnotation(ComputedAnnotationNames.Computeds, computeds);
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

    internal static IReadOnlyList<Computed> GetSortedComputedsOrThrow(this IModel annotatable)
    {
        return annotatable.FindRuntimeAnnotationValue(ComputedAnnotationNames.SortedComputeds) as IReadOnlyList<Computed>
            ?? throw new Exception($"SortedComputeds not found in model");
    }

    internal static void SetSortedComputeds(this IModel annotatable, IReadOnlyList<Computed> computeds)
    {
        annotatable.SetRuntimeAnnotation(ComputedAnnotationNames.SortedComputeds, computeds);
    }
}
