using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace LLL.AutoCompute.EFCore.Metadata.Conventions;

class DesignTimeComputedConvention(
    Func<IModel, IComputedExpressionAnalyzer> analyzerFactory,
    bool enableBackfillInMigrations
) : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        if (!EF.IsDesignTime)
            return;

        var model = modelBuilder.Metadata;

        if (enableBackfillInMigrations)
        {
            // 1. Collect computed factories BEFORE removing annotations
            var factories = new List<(IProperty, ComputedMemberFactory<IProperty>)>();
            foreach (var entityType in model.GetEntityTypes())
            {
                foreach (var property in entityType.GetDeclaredProperties())
                {
                    var factory = ((IReadOnlyProperty)property).GetComputedFactory();
                    if (factory is not null)
                        factories.Add(((IProperty)property, factory));
                }
            }

            DesignTimeComputedStore.Factories = factories;

            // 2. Compute hashes from expanded expressions (after expression modifiers)
            var analyzer = analyzerFactory((IModel)model);
            foreach (var entityType in model.GetEntityTypes())
            {
                foreach (var property in entityType.GetDeclaredProperties())
                {
                    var rawExpr = property.FindAnnotation(AutoComputeAnnotationNames.RawExpression)?.Value as LambdaExpression;
                    if (rawExpr is null)
                        continue;

                    var expanded = analyzer.RunExpressionModifiers(rawExpr);
                    var hash = ComputeHash(expanded.ToString());
                    property.SetAnnotation(AutoComputeAnnotationNames.Hash, hash);
                }
            }
        }

        // 3. Remove non-serializable annotations (original behavior)
        var annotatables = model.GetEntityTypes()
            .SelectMany(e => new IConventionAnnotatable[] { e }.Concat(e.GetMembers().OfType<IConventionAnnotatable>()))
            .Concat([model])
            .ToArray();

        foreach (var annotatable in annotatables)
        {
            var computedAnnotations = annotatable
                .GetAnnotations()
                .Where(a => a.Name.StartsWith(AutoComputeAnnotationNames.TempPrefix))
                .ToArray();

            if (computedAnnotations.Length > 0)
            {
                foreach (var annotation in computedAnnotations)
                    annotatable.RemoveAnnotation(annotation.Name);
            }
        }
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
