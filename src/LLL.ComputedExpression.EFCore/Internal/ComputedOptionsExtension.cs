using System.Linq.Expressions;
using LLL.ComputedExpression.Caching;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;

namespace LLL.ComputedExpression.EFCore.Internal;

public class ComputedOptionsExtension : IDbContextOptionsExtension
{
    public DbContextOptionsExtensionInfo Info => new ComputedOptionsExtensionInfo(this);

    public Func<IServiceProvider, IModel, ComputedExpressionAnalyzer<IEFCoreComputedInput>>? AnalyzerFactory { get; set; }

    public List<Action<IServiceProvider, IModel, ComputedExpressionAnalyzer<IEFCoreComputedInput>>> AnalyzerConfigurations { get; } = [];

    public IList<Func<LambdaExpression, LambdaExpression>> ExpressionModifiers { get; } = [];

    public void ApplyServices(IServiceCollection services)
    {
        services.AddSingleton(s => new Func<IModel, IComputedExpressionAnalyzer<IEFCoreComputedInput>>((model) =>
        {
            var analyzer = AnalyzerFactory is not null
                ? AnalyzerFactory(s, model)
                : DefaultAnalyzerFactory(s, model);

            foreach (var customize in AnalyzerConfigurations)
                customize(s, model, analyzer);

            return analyzer;
        }));

        services.AddSingleton<IConcurrentCreationCache, ConcurrentCreationMemoryCache>();
        services.AddScoped<IConventionSetPlugin, ComputedConventionSetPlugin>();
    }

    public void Validate(IDbContextOptions options)
    {
    }

    private static ComputedExpressionAnalyzer<IEFCoreComputedInput> DefaultAnalyzerFactory(
        IServiceProvider service, IModel model)
    {
        var concurrentCreationCache = service.GetRequiredService<IConcurrentCreationCache>();
        
        return new ComputedExpressionAnalyzer<IEFCoreComputedInput>(
                concurrentCreationCache,
                ExpressionEqualityComparer.Instance
            )
            .AddDefaults()
            .AddEntityMemberAccessLocator(new EFCoreEntityMemberAccessLocator(model))
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
