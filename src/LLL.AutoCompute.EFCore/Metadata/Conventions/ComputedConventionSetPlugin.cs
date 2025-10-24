using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace LLL.AutoCompute.EFCore.Metadata.Conventions;

public class ComputedConventionSetPlugin(
    Func<IModel, IComputedExpressionAnalyzer<EFCoreComputedInput>> analyzerFactory
) : IConventionSetPlugin
{
    public ConventionSet ModifyConventions(ConventionSet conventionSet)
    {
        conventionSet.Add(new RemoveComputedAnnotationsConvention());
        conventionSet.Add(new ComputedRuntimeConvention(analyzerFactory));

        return conventionSet;
    }
}
