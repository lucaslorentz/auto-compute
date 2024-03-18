using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace LLL.Computed.EFCore.Internal;

public class ComputedOptionsExtension : IDbContextOptionsExtension
{
    public DbContextOptionsExtensionInfo Info => new ComputedOptionsExtensionInfo(this);

    public List<Action<IModel, ComputedExpressionAnalyzer<EFCoreAffectedEntitiesInput>>> ConfigureAnalyzer { get; } = [];

    public void ApplyServices(IServiceCollection services)
    {
        services.AddSingleton<Func<IModel, IComputedExpressionAnalyzer>>(model =>
        {
            var analyzer = ComputedExpressionAnalyzer<EFCoreAffectedEntitiesInput>
                .CreateWithDefaults()
                .AddEntityNavigationProvider(new EFCoreEntityNavigationProvider(model))
                .AddEntityChangeTracker(new EFCoreEntityChangeTracker(model));

            foreach (var customize in ConfigureAnalyzer)
                customize(model, analyzer);

            return analyzer;
        });

        services.AddSingleton<IAffectedEntitiesProviderCache, AffectedEntitiesProviderCache>();
        services.AddScoped<IConventionSetPlugin, ComputedConventionSetPlugin>();
    }

    public void Validate(IDbContextOptions options)
    {
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
