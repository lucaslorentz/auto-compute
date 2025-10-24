using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Caching;
using LLL.AutoCompute.EFCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace LLL.AutoCompute.EFCore.Internal;

public class ComputedOptionsExtension : IDbContextOptionsExtension
{
    public DbContextOptionsExtensionInfo Info => new ComputedOptionsExtensionInfo(this);

    public Func<IServiceProvider, IModel, IComputedExpressionAnalyzer<EFCoreComputedInput>>? AnalyzerFactory { get; set; }

    public List<Action<IServiceProvider, IModel, ComputedExpressionAnalyzer<EFCoreComputedInput>>> AnalyzerConfigurations { get; } = [];

    public IList<Func<LambdaExpression, LambdaExpression>> ExpressionModifiers { get; } = [];

    public void ApplyServices(IServiceCollection services)
    {
        services.AddSingleton(s => new Func<IModel, IComputedExpressionAnalyzer<EFCoreComputedInput>>((model) =>
        {
            return AnalyzerFactory is not null
                ? AnalyzerFactory(s, model)
                : DefaultAnalyzerFactory(s, model);
        }));

        services.AddSingleton<IConcurrentCreationCache, ConcurrentCreationMemoryCache>();
        services.AddScoped<IConventionSetPlugin, ComputedConventionSetPlugin>();
    }

    public void Validate(IDbContextOptions options)
    {
    }

    private IComputedExpressionAnalyzer<EFCoreComputedInput> DefaultAnalyzerFactory(
        IServiceProvider service, IModel model)
    {
        var analyzer = new ComputedExpressionAnalyzer<EFCoreComputedInput>()
            .AddDefaults()
            .AddObservedMemberAccessLocator(new EFCoreObservedMemberAccessLocator(model))
            .SetObservedEntityTypeResolver(new EFCoreObservedEntityTypeResolver(model));

        foreach (var customize in AnalyzerConfigurations)
            customize(service, model, analyzer);

        return analyzer;
    }

    private class ComputedOptionsExtensionInfo(IDbContextOptionsExtension extension)
        : DbContextOptionsExtensionInfo(extension)
    {
        public override bool IsDatabaseProvider => false;
        public override string LogFragment => string.Empty;
        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
        }
        public override int GetServiceProviderHashCode() => 0;
        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            => true;
    }
}
