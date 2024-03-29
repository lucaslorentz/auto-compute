using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using LLL.Computed;
using LLL.Computed.AffectedEntitiesProviders;
using LLL.Computed.EFCore.Internal;
using LLL.Computed.Incremental;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

class ComputedRuntimeConvention(Func<IModel, IComputedExpressionAnalyzer> computedExpressionAnalyzerFactory) : IModelFinalizedConvention
{
    public IModel ProcessModelFinalized(IModel model)
    {
        var computedExpressionAnalyzer = computedExpressionAnalyzerFactory(model);

        model.AddRuntimeAnnotation(ComputedAnnotationNames.ExpressionAnalyzer, computedExpressionAnalyzer);

        var computedUpdaters = new List<Func<DbContext, Task<int>>>();

        foreach (var entityType in model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var computedDefinition = property.FindAnnotation(ComputedAnnotationNames.Expression)?.Value;
                if (computedDefinition is LambdaExpression computedExpression)
                {
                    CreatedComputedExpressionUpdaters(
                        computedExpressionAnalyzer,
                        computedUpdaters,
                        property,
                        computedExpression);
                }
                else if (computedDefinition is IIncrementalComputed incrementalComputed)
                {
                    CreatedIncrementalComputedUpdaters(
                        computedExpressionAnalyzer,
                        computedUpdaters,
                        property,
                        incrementalComputed);
                }
            }
        }

        model.AddRuntimeAnnotation(ComputedAnnotationNames.Updaters, computedUpdaters);

        return model;
    }

    private static void CreatedComputedExpressionUpdaters(
        IComputedExpressionAnalyzer computedExpressionAnalyzer,
        List<Func<DbContext, Task<int>>> computedUpdaters,
        IProperty property,
        LambdaExpression computedExpression)
    {
        IAffectedEntitiesProvider affectedEntitiesProvider;
        try
        {
            affectedEntitiesProvider = computedExpressionAnalyzer.CreateAffectedEntitiesProvider(computedExpression);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Invalid computed expression for '{property.DeclaringEntityType.ShortName()}.{property.Name}': {ex.Message}");
        }

        var compiledExpression = computedExpression.Compile();

        computedUpdaters.Add(async (dbContext) =>
        {
            var changes = 0;
            foreach (var affectedEntity in await affectedEntitiesProvider.GetAffectedEntitiesAsync(new EFCoreComputedInput(dbContext)))
            {
                var affectedEntry = dbContext.Entry(affectedEntity);
                var propertyEntry = affectedEntry.Property(property);
                var newValue = compiledExpression.DynamicInvoke(affectedEntity);
                var valueComparer = property.GetValueComparer();
                if (!valueComparer.Equals(propertyEntry.CurrentValue, newValue))
                {
                    propertyEntry.CurrentValue = newValue;
                    changes++;
                }
            }
            return changes;
        });
    }
    private static void CreatedIncrementalComputedUpdaters(
        IComputedExpressionAnalyzer computedExpressionAnalyzer,
        List<Func<DbContext, Task<int>>> computedUpdaters,
        IProperty property,
        IIncrementalComputed incrementalComputed)
    {
        try
        {
            foreach (var incrementalPart in incrementalComputed.Parts)
            {
                var entityContext = computedExpressionAnalyzer.GetEntityContext(
                    incrementalPart.Navigation,
                    incrementalPart.Navigation.Body,
                    incrementalPart.IsMany ? EntityContextKeys.Element : EntityContextKeys.None);

                var valueAffectedEntitiesProvider = computedExpressionAnalyzer.CreateAffectedEntitiesProvider(incrementalPart.ValueExtraction);
                var rootRelationshipAffectedEntitiesProvider = entityContext.GetAffectedEntitiesProviderInverse();
                var originalValueGetter = computedExpressionAnalyzer.GetOriginalValueExpression(incrementalPart.ValueExtraction).Compile();
                var currentValueGetter = incrementalPart.ValueExtraction.Compile();

                var composedAffectedEntitiesProvider = CompositeAffectedEntitiesProvider.ComposeIfNecessary([
                    valueAffectedEntitiesProvider,
                    rootRelationshipAffectedEntitiesProvider
                ]);

                computedUpdaters.Add(async (dbContext) =>
                {
                    var incrementalChanges = GetIncrementalChanges(dbContext);

                    var changes = 0;
                    var input = new EFCoreComputedInput(dbContext);
                    foreach (var affectedEntity in await composedAffectedEntitiesProvider.GetAffectedEntitiesAsync(input))
                    {
                        var affectedEntry = dbContext.Entry(affectedEntity);

                        var incrementalChangeKey = new IncrementalChangeKey(incrementalPart, affectedEntity);

                        object? oldPartValue;
                        IEnumerable<object> oldRoots;
                        if (incrementalChanges.TryGetValue(incrementalChangeKey, out var incrementalChangeValue))
                        {
                            oldPartValue = incrementalChangeValue.Value;
                            oldRoots = incrementalChangeValue.Roots;
                        }
                        else
                        {
                            oldPartValue = affectedEntry.State == EntityState.Added ? incrementalComputed.DefaultValue : originalValueGetter.DynamicInvoke(input, affectedEntity);
                            oldRoots = affectedEntry.State == EntityState.Added ? [] : await entityContext.LoadOriginalRootEntities(input, [affectedEntity]);
                        }

                        var newPartValue = affectedEntry.State == EntityState.Deleted ? incrementalComputed.DefaultValue : currentValueGetter.DynamicInvoke(affectedEntity);
                        var newRoots = affectedEntry.State == EntityState.Deleted ? [] : await entityContext.LoadCurrentRootEntities(input, [affectedEntity]);

                        foreach (var rootEntity in oldRoots.Except(newRoots))
                        {
                            var rootEntry = dbContext.Entry(rootEntity);
                            var propertyEntry = rootEntry.Property(property);
                            var valueComparer = property.GetValueComparer();

                            var newComputedValue = incrementalPart.Update.DynamicInvoke(
                                propertyEntry.CurrentValue,
                                oldPartValue,
                                incrementalComputed.DefaultValue);

                            if (!valueComparer.Equals(propertyEntry.CurrentValue, newComputedValue))
                            {
                                propertyEntry.CurrentValue = newComputedValue;
                                changes++;
                            }
                        }

                        foreach (var rootEntity in newRoots)
                        {
                            var rootEntry = dbContext.Entry(rootEntity);
                            var propertyEntry = rootEntry.Property(property);
                            var valueComparer = property.GetValueComparer();

                            var newComputedValue = incrementalPart.Update.DynamicInvoke(
                                propertyEntry.CurrentValue,
                                oldRoots.Contains(rootEntity)
                                    ? oldPartValue
                                    : incrementalComputed.DefaultValue,
                                newPartValue);

                            if (!valueComparer.Equals(propertyEntry.CurrentValue, newComputedValue))
                            {
                                propertyEntry.CurrentValue = newComputedValue;
                                changes++;
                            }
                        }

                        incrementalChanges[incrementalChangeKey] = new IncrementalChangeValues(newPartValue, newRoots);
                    }
                    return changes;
                });
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Invalid computed expression for '{property.DeclaringEntityType.ShortName()}.{property.Name}': {ex.Message}");
        }
    }

    record IncrementalChangeKey(
        IncrementalComputedPart IncrementalPart,
        object AffectedEntity
    );

    record IncrementalChangeValues(
        object? Value,
        IEnumerable<object> Roots
    );

    static readonly ConditionalWeakTable<DbContext, Dictionary<IncrementalChangeKey, IncrementalChangeValues>> _incrementalChanges = new();
    static Dictionary<IncrementalChangeKey, IncrementalChangeValues> GetIncrementalChanges(DbContext dbContext)
    {
        return _incrementalChanges.GetOrCreateValue(dbContext);
    }
}
