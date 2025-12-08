using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace LLL.AutoCompute.EFCore.Metadata.Conventions;

class ComputedRuntimeConvention(Func<IModel, IComputedExpressionAnalyzer> analyzerFactory)
    : IModelFinalizedConvention
{
    public IModel ProcessModelFinalized(IModel model)
    {
        var analyzer = analyzerFactory(model);

        model.SetExpressionAnalyzer(analyzer);

        var allComputedMembers = CreateComputedMembers(model, analyzer)
            .TopoSort(c => c.GetComputedDependencies());

        model.SetAllComputedMembers(allComputedMembers);

        var allComputedObservers = CreateComputedObservers(model, analyzer);

        model.SetAllComputedObservers(allComputedObservers);

        return model;
    }

    private static IReadOnlyList<ComputedMember> CreateComputedMembers(
        IModel model,
        IComputedExpressionAnalyzer analyzer)
    {
        var allComputedMembers = new List<ComputedMember>();

        foreach (var entityType in model.GetEntityTypes())
        {
            foreach (var property in entityType.GetDeclaredProperties())
            {
                var computedFacotry = property.GetComputedFactory();
                if (computedFacotry is not null)
                {
                    var computed = computedFacotry(analyzer, property);
                    property.SetComputedMember(computed);
                    allComputedMembers.Add(computed);
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
                    navigation.SetComputedMember(computed);
                    allComputedMembers.Add(computed);
                }
            }
        }

        foreach (var computedMember in allComputedMembers)
        {
            ValidateSelfReferencingComputedMember(computedMember);

            foreach (var observedMember in computedMember.ObservedMembers)
                observedMember.AddDependentMember(computedMember);
        }

        return allComputedMembers;
    }

    private static IReadOnlyList<ComputedObserver> CreateComputedObservers(
        IModel model,
        IComputedExpressionAnalyzer analyzer)
    {
        var allComputedObservers = new List<ComputedObserver>();

        foreach (var entityType in model.GetEntityTypes())
        {
            var computedObserversFactories = entityType.GetObserversFactories();
            if (computedObserversFactories is not null)
            {
                var computedObservers = computedObserversFactories
                    .Select(f => f(analyzer, entityType))
                    .ToArray();
                entityType.SetComputedObservers(computedObservers);
                allComputedObservers.AddRange(computedObservers);
            }
        }

        return allComputedObservers;
    }

    private static void ValidateSelfReferencingComputedMember(ComputedMember computedMember)
    {
        var observedMember = computedMember.Property.GetObservedMember();
        if (observedMember is not null
            && computedMember.ChangesProvider.EntityContext.ObservedMembers.Contains(observedMember))
        {
            throw new Exception($"Clyclic computed expression in {computedMember.ToDebugString()}");
        }
    }
}
