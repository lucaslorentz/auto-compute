using LLL.ComputedExpression.EFCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.ComputedExpression.EFCore;

public class ComputedOptionsBuilder
{
    private readonly ComputedOptionsExtension _extension;

    public ComputedOptionsBuilder(DbContextOptionsBuilder optionsBuilder)
    {
        _extension = optionsBuilder.Options.FindExtension<ComputedOptionsExtension>()
            ?? new ComputedOptionsExtension();

        optionsBuilder.AddInterceptors(new ComputedInterceptor());
    }

    public ComputedOptionsBuilder AnalyzerFactory(Func<IModel, ComputedExpressionAnalyzer<IEFCoreComputedInput>>? factory)
    {
        _extension.AnalyzerFactory = factory;
        return this;
    }

    public ComputedOptionsBuilder ConfigureAnalyzer(Action<IModel, ComputedExpressionAnalyzer<IEFCoreComputedInput>> configuration)
    {
        _extension.AnalyzerConfigurations.Add(configuration);
        return this;
    }

    internal ComputedOptionsExtension Build()
    {
        return _extension;
    }
}
