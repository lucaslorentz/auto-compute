using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore;

public class ComputedOptionsBuilder
{
    private readonly ComputedOptionsExtension _extension;
    private readonly DbContextOptionsBuilder _optionsBuilder;
    private bool _enableUpdateComputedsOnSave = true;
    private bool _enableNotifyObserversOnSave = true;

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

    public ComputedOptionsBuilder EnableNotifyObserversOnSave(bool enable)
    {
        _enableNotifyObserversOnSave = enable;
        return this;
    }

    internal ComputedOptionsExtension Build()
    {
        _optionsBuilder.AddInterceptors(new ComputedSaveChangesInterceptor(_enableUpdateComputedsOnSave, _enableNotifyObserversOnSave));
        return _extension;
    }
}
