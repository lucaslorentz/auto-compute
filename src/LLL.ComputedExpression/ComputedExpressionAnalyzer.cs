using System.Linq.Expressions;
using LLL.ComputedExpression.AffectedEntitiesProviders;
using LLL.ComputedExpression.ChangesProvider;
using LLL.ComputedExpression.EntityContextPropagators;
using LLL.ComputedExpression.EntityContexts;
using LLL.ComputedExpression.ExpressionVisitors;
using LLL.ComputedExpression.Incremental;
using LLL.ComputedExpression.IncrementalChangesProvider;
using LLL.ComputedExpression.Internal;

namespace LLL.ComputedExpression;

public class ComputedExpressionAnalyzer<TInput> : IComputedExpressionAnalyzer
{
    private readonly IList<IEntityContextPropagator> _entityContextPropagators = [];
    private readonly HashSet<IEntityNavigationAccessLocator> _navigationAccessLocators = [];
    private readonly HashSet<IEntityMemberAccessLocator> _memberAccessLocators = [];
    private IEntityActionProvider<TInput>? _entityActionProvider;

    private ComputedExpressionAnalyzer() { }

    public static ComputedExpressionAnalyzer<TInput> Create()
    {
        return new ComputedExpressionAnalyzer<TInput>();
    }

    public static ComputedExpressionAnalyzer<TInput> CreateWithDefaults()
    {
        return Create().AddDefaults();
    }

    public ComputedExpressionAnalyzer<TInput> AddDefaults()
    {
        return AddEntityContextPropagator(new LinqMethodsEntityContextPropagator())
            .AddEntityContextPropagator(new KeyValuePairEntityContextPropagator())
            .AddEntityContextPropagator(new GroupingEntityContextPropagator())
            .AddEntityContextPropagator(new NavigationEntityContextPropagator(_navigationAccessLocators))
            .AddStopTrackingDecision(new StopTrackingDecision());
    }

    public ComputedExpressionAnalyzer<TInput> AddStopTrackingDecision(
        IStopTrackingDecision stopTrackingDecision)
    {
        _entityContextPropagators.Insert(0, new UntrackedEntityContextPropagator<TInput>(
            stopTrackingDecision
        ));
        return this;
    }

    public ComputedExpressionAnalyzer<TInput> AddEntityMemberAccessLocator(
        IEntityMemberAccessLocator<TInput> memberAccessLocator)
    {
        if (memberAccessLocator is IEntityNavigationAccessLocator nav)
            _navigationAccessLocators.Add(nav);

        _memberAccessLocators.Add(memberAccessLocator);
        return this;
    }

    public ComputedExpressionAnalyzer<TInput> AddEntityContextPropagator(
        IEntityContextPropagator propagator)
    {
        _entityContextPropagators.Add(propagator);
        return this;
    }

    public ComputedExpressionAnalyzer<TInput> SetEntityActionProvider(
        IEntityActionProvider<TInput> entityActionProvider
    )
    {
        _entityActionProvider = entityActionProvider;
        return this;
    }

    public IAffectedEntitiesProvider? CreateAffectedEntitiesProvider(LambdaExpression computed)
    {
        var entityContext = GetEntityContext(computed, computed.Parameters[0], EntityContextKeys.None);
        return entityContext.GetAffectedEntitiesProvider();
    }

    public IChangesProvider? CreateChangesProvider(LambdaExpression computed)
    {
        var affectedEntitiesProvider = CreateAffectedEntitiesProvider(computed);
        if (affectedEntitiesProvider is null)
            return null;
        var originalValueGetter = GetOriginalValueExpression(computed).Compile();
        var currentValueGetter = GetCurrentValueExpression(computed).Compile();
        var entityActionProvider = RequireEntityActionProvider();

        var entityType = computed.Parameters[0].Type;
        var valueType = computed.Body.Type;

        var closedType = typeof(DefaultChangesProvider<,,>)
            .MakeGenericType(typeof(TInput), entityType, valueType);

        return (IChangesProvider)Activator.CreateInstance(
            closedType,
            affectedEntitiesProvider,
            originalValueGetter,
            currentValueGetter,
            entityActionProvider)!;
    }

    public LambdaExpression GetOriginalValueExpression(LambdaExpression computed)
    {
        var inputParameter = Expression.Parameter(typeof(TInput), "input");

        var newBody = new ChangeToOriginalValueVisitor(
            inputParameter,
            _memberAccessLocators
        ).Visit(computed.Body)!;

        return Expression.Lambda(newBody, [inputParameter, .. computed.Parameters]);
    }

    public LambdaExpression GetCurrentValueExpression(LambdaExpression computed)
    {
        var inputParameter = Expression.Parameter(typeof(TInput), "input");

        var newBody = new ChangeToCurrentValueVisitor(
            inputParameter,
            _memberAccessLocators
        ).Visit(computed.Body)!;

        return Expression.Lambda(newBody, [inputParameter, .. computed.Parameters]);
    }

    public IIncrementalChangesProvider CreateIncrementalChangesProvider(
        IIncrementalComputed incrementalComputed)
    {
        var providers = new List<IIncrementalChangesProvider>();

        foreach (var incrementalPart in incrementalComputed.Parts)
        {
            var provider = CreatePartIncrementalChangesProvider(incrementalPart);
            if (provider is not null)
                providers.Add(provider);
        }

        var providerType = typeof(IIncrementalChangesProvider<,,>)
            .MakeGenericType(typeof(TInput), incrementalComputed.EntityType, incrementalComputed.ValueType);

        var convertedCleanupUpProviders = providers.ToArray(providerType);

        var closedType = typeof(CompositeIncrementalChangesProvider<,,>)
            .MakeGenericType(typeof(TInput), incrementalComputed.EntityType, incrementalComputed.ValueType);

        return (IIncrementalChangesProvider)Activator.CreateInstance(closedType, incrementalComputed, convertedCleanupUpProviders)!;

        IIncrementalChangesProvider? CreatePartIncrementalChangesProvider(
            IncrementalComputedPart incrementalComputedPart)
        {
            var entityActionProvider = RequireEntityActionProvider();

            var entityContext = GetEntityContext(
                incrementalComputedPart.Navigation,
                incrementalComputedPart.Navigation.Body,
                incrementalComputedPart.IsMany ? EntityContextKeys.Element : EntityContextKeys.None);

            var valueAffectedEntitiesProvider = CreateAffectedEntitiesProvider(incrementalComputedPart.ValueSelector);
            var rootRelationshipAffectedEntitiesProvider = entityContext.GetAffectedEntitiesProviderInverse();
            var originalValueGetter = GetOriginalValueExpression(incrementalComputedPart.ValueSelector).Compile();
            var currentValueGetter = GetCurrentValueExpression(incrementalComputedPart.ValueSelector).Compile();
            var originalRootEntitiesProvider = entityContext.GetOriginalRootEntitiesProvider();
            var currentRootEntitiesProvider = entityContext.GetCurrentRootEntitiesProvider();

            var composedAffectedEntitiesProvider = AffectedEntitiesProvider.ComposeAndCleanup([
                valueAffectedEntitiesProvider,
                rootRelationshipAffectedEntitiesProvider
            ]);

            if (composedAffectedEntitiesProvider is null)
                return null;

            var partEntityType = incrementalComputedPart.ValueSelector.Parameters[0].Type;

            var closedType = typeof(PartIncrementalChangesProvider<,,,>)
                .MakeGenericType(typeof(TInput), incrementalComputed.EntityType, incrementalComputed.ValueType, partEntityType);

            return (IIncrementalChangesProvider)Activator.CreateInstance(
                closedType,
                incrementalComputed,
                composedAffectedEntitiesProvider,
                entityActionProvider,
                originalValueGetter,
                currentValueGetter,
                originalRootEntitiesProvider,
                currentRootEntitiesProvider
            )!;
        }
    }

    private EntityContext GetEntityContext(
        LambdaExpression computed,
        Expression node,
        string entityContextKey)
    {
        var rootEntityType = computed.Parameters[0].Type;

        var analysis = new ComputedExpressionAnalysis(this, rootEntityType);

        var entityContext = new RootEntityContext(typeof(TInput), rootEntityType);

        analysis.AddEntityContextProvider(computed.Parameters[0], (key) => key == EntityContextKeys.None ? entityContext : null);

        new PropagateEntityContextsVisitor(
            _entityContextPropagators,
            analysis
        ).Visit(computed);

        new CollectEntityMemberAccessesVisitor(
            analysis,
            _memberAccessLocators
        ).Visit(computed);

        return analysis.ResolveEntityContext(node, entityContextKey);
    }

    private IEntityActionProvider<TInput> RequireEntityActionProvider()
    {
        return _entityActionProvider
            ?? throw new Exception("Entity Action Provider not configured");
    }
}
