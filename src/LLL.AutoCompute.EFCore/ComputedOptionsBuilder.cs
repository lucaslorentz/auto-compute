using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore;

public class ComputedOptionsBuilder
{
    private readonly ComputedOptionsExtension _extension;
    private readonly DbContextOptionsBuilder _optionsBuilder;
    private bool _enableUpdateComputedsOnSave = true;

    public ComputedOptionsBuilder(DbContextOptionsBuilder optionsBuilder)
    {
        _extension = optionsBuilder.Options.FindExtension<ComputedOptionsExtension>()
            ?? new ComputedOptionsExtension();

        _optionsBuilder = optionsBuilder;
    }

    public ComputedOptionsBuilder AnalyzerFactory(Func<IServiceProvider, IModel, ComputedExpressionAnalyzer<IEFCoreComputedInput>>? factory)
    {
        _extension.AnalyzerFactory = factory;
        return this;
    }

    public ComputedOptionsBuilder ConfigureAnalyzer(Action<IServiceProvider, IModel, ComputedExpressionAnalyzer<IEFCoreComputedInput>> configuration)
    {
        _extension.AnalyzerConfigurations.Add(configuration);
        return this;
    }

    public ComputedOptionsBuilder EnableUpdateComputedsOnSave(bool enable)
    {
        _enableUpdateComputedsOnSave = enable;
        return this;
    }

    internal ComputedOptionsExtension Build()
    {
        _optionsBuilder.AddInterceptors(new ComputedInterceptor(_enableUpdateComputedsOnSave));
        return _extension;
    }
}
