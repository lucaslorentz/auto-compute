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
        {
            // TODO: Validate if forbid cyclic dependencies is enabled
            // ValidateCyclicComputedDependencies(computed, computed, []);

            if (computed is ComputedMember computedMember)
                ValidateSelfReferencingComputed(computedMember);

            foreach (var observedMember in computed.ObservedMembers)
                observedMember.AddDependent(computed);
        }

        var sortedComputeds = computeds.TopoSort(c => c.GetComputedDependencies());

        model.SetSortedComputeds(sortedComputeds);

        return model;
    }

    private static IReadOnlyList<ComputedBase> CreateComputeds(
        IModel model,
        IComputedExpressionAnalyzer<IEFCoreComputedInput> analyzer)
    {
        var computeds = new List<ComputedBase>();

        foreach (var entityType in model.GetEntityTypes())
        {
            var observersFactories = entityType.GetObserversFactories();
            if (observersFactories is not null)
            {
                var observers = observersFactories
                    .Select(f => f(analyzer, entityType))
                    .OfType<ComputedObserver>()
                    .ToArray();
                entityType.SetObservers(observers);
                computeds.AddRange(observers);
            }

            foreach (var property in entityType.GetDeclaredProperties())
            {
                var computedFacotry = property.GetComputedFactory();
                if (computedFacotry is not null)
                {
                    var computed = computedFacotry(analyzer, property);
                    property.SetComputed(computed);
                    computeds.Add(computed);
                }
            }

            var navigations = entityType.GetDeclaredNavigations()
                .OfType<INavigationBase>()
                .Concat(entityType.GetDeclaredSkipNavigations());

            foreach (var navigation in navigations)
            {
                var computedFactory = navigation.GetComputedFactory();
                if (computedFactory is not null)
                {
                    var computed = computedFactory(analyzer, navigation);
                    navigation.SetComputed(computed);
                    computeds.Add(computed);
                }
            }
        }

        return computeds;
    }

    private static void ValidateCyclicComputedDependencies(
        ComputedBase initial,
        ComputedBase current,
        HashSet<ComputedBase> visited)
    {
        visited.Add(current);

        foreach (var dependency in current.GetComputedDependencies())
        {
            if (visited.Contains(dependency))
                throw new Exception($"Cyclic computed dependency between {initial.ToDebugString()} and {current.ToDebugString()}");

            ValidateCyclicComputedDependencies(initial, dependency, visited);
        }
    }

    private static void ValidateSelfReferencingComputed(ComputedMember computedMember)
    {
        var observedMember = computedMember.Property.GetObservedMember();
        if (observedMember is not null
            && computedMember.ChangesProvider.EntityContext.ObservedMembers.Contains(observedMember))
        {
            throw new Exception($"Clyclic computed expression in {computedMember.ToDebugString()}");
        }
    }
}
