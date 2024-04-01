using LLL.ComputedExpression.Caching;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace LLL.ComputedExpression.EFCore.Internal;

public class ComputedOptionsExtension : IDbContextOptionsExtension
{
    public DbContextOptionsExtensionInfo Info => new ComputedOptionsExtensionInfo(this);

    public Func<IModel, ComputedExpressionAnalyzer<IEFCoreComputedInput>>? AnalyzerFactory { get; set; }

    public List<Action<IModel, ComputedExpressionAnalyzer<IEFCoreComputedInput>>> AnalyzerConfigurations { get; } = [];

    public void ApplyServices(IServiceCollection services)
    {
        services.AddSingleton<Func<IModel, IComputedExpressionAnalyzer>>(model =>
        {
            var analyzer = AnalyzerFactory is not null
                ? AnalyzerFactory(model)
                : DefaultAnalyzerFactory(model);

            foreach (var customize in AnalyzerConfigurations)
                customize(model, analyzer);

            return analyzer;
        });

        services.AddSingleton<IConcurrentCreationCache, ConcurrentCreationMemoryCache>();
        services.AddScoped<IConventionSetPlugin, ComputedConventionSetPlugin>();
    }

    public void Validate(IDbContextOptions options)
    {
    }

    private static ComputedExpressionAnalyzer<IEFCoreComputedInput> DefaultAnalyzerFactory(IModel model)
    {
        return ComputedExpressionAnalyzer<IEFCoreComputedInput>
            .CreateWithDefaults()
            .AddEntityMemberAccessLocator(new EFCoreMemberAccessLocator(model))
            .SetEntityActionProvider(new EFCoreEntityActionProvider());
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
