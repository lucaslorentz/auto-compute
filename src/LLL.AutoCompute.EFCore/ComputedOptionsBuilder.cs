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
    private bool _enableMigrationBackfill = false;

    public ComputedOptionsBuilder(DbContextOptionsBuilder optionsBuilder)
    {
        _extension = optionsBuilder.Options.FindExtension<ComputedOptionsExtension>()
            ?? new ComputedOptionsExtension();

        _optionsBuilder = optionsBuilder;
    }

    public ComputedOptionsBuilder AnalyzerFactory(Func<IServiceProvider, IModel, ComputedExpressionAnalyzer>? factory)
    {
        _extension.AnalyzerFactory = factory;
        return this;
    }

    public ComputedOptionsBuilder ConfigureAnalyzer(Action<IServiceProvider, IModel, ComputedExpressionAnalyzer> configuration)
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

    public ComputedOptionsBuilder EnableBackfillInMigrations(bool enable = true)
    {
        _enableMigrationBackfill = enable;
        return this;
    }

    internal ComputedOptionsExtension Build()
    {
        _extension.EnableBackfillInMigrations = _enableMigrationBackfill;
        var interceptors = new List<Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor>
        {
            new ComputedSaveChangesInterceptor(_enableUpdateComputedsOnSave, _enableNotifyObserversOnSave)
        };
        if (_enableMigrationBackfill && EF.IsDesignTime)
            interceptors.Add(new SqlCaptureInterceptor());
        _optionsBuilder.AddInterceptors(interceptors);
        return _extension;
    }
}
