using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace LLL.AutoCompute.EFCore.Internal;

public class ComputedConventionSetPlugin(
    Func<IModel, IComputedExpressionAnalyzer<IEFCoreComputedInput>> analyzerFactory
) : IConventionSetPlugin
{
    public ConventionSet ModifyConventions(ConventionSet conventionSet)
    {
        conventionSet.Add(new ComputedRuntimeConvention(analyzerFactory));

        return conventionSet;
    }
}
