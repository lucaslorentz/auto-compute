using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace LLL.AutoCompute.EFCore.Metadata.Conventions;

public class ComputedConventionSetPlugin(
    Func<IModel, IComputedExpressionAnalyzer> analyzerFactory,
    IDbContextOptions contextOptions
) : IConventionSetPlugin
{
    public ConventionSet ModifyConventions(ConventionSet conventionSet)
    {
        var extension = contextOptions.FindExtension<ComputedOptionsExtension>();
        var enableBackfill = extension?.EnableBackfillInMigrations ?? false;

        conventionSet.Add(new DesignTimeComputedConvention(analyzerFactory, enableBackfill));
        conventionSet.Add(new ComputedRuntimeConvention(analyzerFactory));

        return conventionSet;
    }
}
