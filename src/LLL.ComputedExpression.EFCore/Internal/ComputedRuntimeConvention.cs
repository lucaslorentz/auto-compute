using LLL.ComputedExpression;
using LLL.ComputedExpression.EFCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

class ComputedRuntimeConvention(Func<IModel, IComputedExpressionAnalyzer<IEFCoreComputedInput>> analyzerFactory) : IModelFinalizedConvention
{
    public IModel ProcessModelFinalized(IModel model)
    {
        var analyzer = analyzerFactory(model);

        model.AddRuntimeAnnotation(ComputedAnnotationNames.ExpressionAnalyzer, analyzer);

        var computedUpdaters = new List<ComputedUpdater>();

        foreach (var entityType in model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.FindAnnotation(ComputedAnnotationNames.UpdaterFactory)?.Value is ComputedUpdaterFactory computedUpdaterFactory)
                {
                    computedUpdaters.Add(
                        computedUpdaterFactory(analyzer, property)
                    );
                }
            }
        }

        model.AddRuntimeAnnotation(ComputedAnnotationNames.Updaters, computedUpdaters);

        return model;
    }
}
