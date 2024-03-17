using System.Linq.Expressions;
using L3.Computed;
using L3.Computed.EFCore.Internal;
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
                if (property.FindAnnotation(ComputedAnnotationNames.Expression)?.Value is LambdaExpression computedExpression)
                {
                    IAffectedEntitiesProvider affectedEntitiesProvider;
                    try
                    {
                        affectedEntitiesProvider = computedExpressionAnalyzer.CreateAffectedEntitiesProvider(computedExpression);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Invalid computed expression for '{entityType.ShortName()}.{property.Name}': {ex.Message}");
                    }

                    var compiledExpression = computedExpression.Compile();

                    computedUpdaters.Add(async (dbContext) =>
                    {
                        var changes = 0;
                        foreach (var affectedEntity in await affectedEntitiesProvider.GetAffectedEntitiesAsync(new EFCoreAffectedEntitiesInput(dbContext)))
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
            }
        }

        model.AddRuntimeAnnotation(ComputedAnnotationNames.Updaters, computedUpdaters);

        return model;
    }
}
