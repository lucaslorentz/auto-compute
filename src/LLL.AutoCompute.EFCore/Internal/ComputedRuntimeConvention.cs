using LLL.AutoCompute;
using LLL.AutoCompute.EFCore;
using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

class ComputedRuntimeConvention(Func<IModel, IComputedExpressionAnalyzer<IEFCoreComputedInput>> analyzerFactory) : IModelFinalizedConvention
{
    public IModel ProcessModelFinalized(IModel model)
    {
        var analyzer = analyzerFactory(model);
        model.AddRuntimeAnnotation(ComputedAnnotationNames.ExpressionAnalyzer, analyzer);

        var computedProperties = model.GetEntityTypes()
            .SelectMany(e => e.GetProperties())
            .Select(p => p.GetComputedProperty())
            .Where(c => c is not null)
            .Select(c => c!)
            .ToArray();

        foreach (var computedProperty in computedProperties)
            ValidateCyclicComputedDependencies(computedProperty, computedProperty);

        var sortedComputedProperties = computedProperties.TopoSort(c => c.GetDependencies());

        model.AddRuntimeAnnotation(ComputedAnnotationNames.SortedProperties, sortedComputedProperties);

        return model;
    }

    private static void ValidateCyclicComputedDependencies(
        ComputedProperty initialComputedProperty,
        ComputedProperty current)
    {
        foreach (var dependency in current.GetDependencies())
        {
            if (Equals(dependency, initialComputedProperty))
                throw new Exception($"Cyclic computed dependency between {initialComputedProperty.Property} and {dependency.Property}");

            ValidateCyclicComputedDependencies(initialComputedProperty, dependency);
        }
    }
}
