using LLL.AutoCompute.EFCore.Internal;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace LLL.AutoCompute.EFCore.Metadata.Conventions;

class ComputedRuntimeConvention(Func<IModel, IComputedExpressionAnalyzer<IEFCoreComputedInput>> analyzerFactory)
    : IModelFinalizedConvention
{
    public IModel ProcessModelFinalized(IModel model)
    {
        var analyzer = analyzerFactory(model);

        model.SetExpressionAnalyzer(analyzer);

        var computeds = CreateComputeds(model, analyzer);

        foreach (var computed in computeds)
            ValidateCyclicComputedDependencies(computed, computed, []);

        var sortedComputeds = computeds.TopoSort(c => c.GetComputedDependencies());

        model.SetSortedComputeds(sortedComputeds);

        return model;
    }

    private static IReadOnlyList<Computed> CreateComputeds(
        IModel model,
        IComputedExpressionAnalyzer<IEFCoreComputedInput> analyzer)
    {
        var computeds = new List<Computed>();

        foreach (var entityType in model.GetEntityTypes())
        {
            var entityTypeFactories = entityType.GetComputedFactories();
            if (entityTypeFactories is not null)
            {
                var entityTypeComputeds = entityTypeFactories
                    .Select(f => f(analyzer, entityType))
                    .ToArray();
                entityType.SetComputeds(entityTypeComputeds);
                computeds.AddRange(entityTypeComputeds);
            }

            foreach (var property in entityType.GetDeclaredProperties())
            {
                var propertyFactories = property.GetComputedFactories();
                if (propertyFactories is not null)
                {
                    var propertyComputeds = propertyFactories
                        .Select(f => f(analyzer, property))
                        .ToArray();
                    property.SetComputeds(propertyComputeds);
                    computeds.AddRange(propertyComputeds);
                }
            }

            foreach (var navigation in entityType.GetDeclaredNavigations())
            {
                var navigationFactories = navigation.GetComputedFactories();
                if (navigationFactories is not null)
                {
                    var navigationComputeds = navigationFactories
                        .Select(f => f(analyzer, navigation))
                        .ToArray();
                    navigation.SetComputeds(navigationComputeds);
                    computeds.AddRange(navigationComputeds);
                }
            }
        }

        return computeds;
    }

    private static void ValidateCyclicComputedDependencies(
        Computed initial,
        Computed current,
        HashSet<Computed> visited)
    {
        visited.Add(current);
        
        foreach (var dependency in current.GetComputedDependencies())
        {
            if (visited.Contains(dependency))
                throw new Exception($"Cyclic computed dependency between {initial.ToDebugString()} and {current.ToDebugString()}");

            ValidateCyclicComputedDependencies(initial, dependency, visited);
        }
    }
}
