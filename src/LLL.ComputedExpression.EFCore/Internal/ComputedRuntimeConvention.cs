using System.Linq.Expressions;
using LLL.ComputedExpression;
using LLL.ComputedExpression.EFCore.Internal;
using LLL.ComputedExpression.Incremental;
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
        try
        {
            var affectedEntitiesProvider = computedExpressionAnalyzer.CreateAffectedEntitiesProvider(computedExpression);

            if (affectedEntitiesProvider is null)
                return;

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
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Invalid computed expression for '{property.DeclaringType.ShortName()}.{property.Name}': {ex.Message}");
        }
    }

    private static void CreatedIncrementalComputedUpdaters(
        IComputedExpressionAnalyzer computedExpressionAnalyzer,
        List<Func<DbContext, Task<int>>> computedUpdaters,
        IProperty property,
        IIncrementalComputed incrementalComputed)
    {
        try
        {
            var incrementalChangeProvider = computedExpressionAnalyzer.CreateIncrementalChangesProvider(
                incrementalComputed);

            computedUpdaters.Add(async (dbContext) =>
            {
                var changes = 0;
                var input = new EFCoreComputedInput(dbContext);
                var incrementalChanges = await incrementalChangeProvider.GetIncrementalChangesAsync(input);
                foreach (var (entity, value) in incrementalChanges)
                {
                    var entityEntry = dbContext.Entry(entity);
                    var propertyEntry = entityEntry.Property(property);

                    var originalValue = entityEntry.State == EntityState.Added
                        ? incrementalComputed.Zero
                        : propertyEntry.OriginalValue ?? incrementalComputed.Zero;

                    var newValue = incrementalComputed.Add(
                        originalValue,
                        value
                    );

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
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Invalid computed expression for '{property.DeclaringType.ShortName()}.{property.Name}': {ex.Message}", ex);
        }
    }
}
