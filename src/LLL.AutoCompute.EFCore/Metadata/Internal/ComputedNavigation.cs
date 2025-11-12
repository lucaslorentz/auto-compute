using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public class ComputedNavigation<TEntity, TProperty>(
    INavigationBase navigationBase,
    IComputedChangesProvider<IEFCoreComputedInput, TEntity, TProperty> changesProvider,
    IReadOnlySet<IPropertyBase> controlledMembers
) : ComputedMember<TEntity, TProperty>(changesProvider), IComputedNavigationBuilder<TEntity, TProperty>
    where TEntity : class
{
    private readonly Func<TEntity, TProperty> _compiledExpression = ((Expression<Func<TEntity, TProperty>>)changesProvider.Expression).Compile();

    public new IComputedChangesProvider<IEFCoreComputedInput, TEntity, TProperty> ChangesProvider => changesProvider;
    public override INavigationBase Property => navigationBase;
    public IReadOnlySet<IPropertyBase> ControlledMembers => controlledMembers;
    public Delegate? ReuseKeySelector { get; set; }

    public override async Task<EFCoreChangeset> Update(IEFCoreComputedInput input)
    {
        var updateChanges = new EFCoreChangeset();
        var changes = await changesProvider.GetChangesAsync(input, null);
        foreach (var (entity, change) in changes)
        {
            var entityEntry = input.DbContext.Entry(entity);
            var navigationEntry = entityEntry.Navigation(navigationBase);

            var originalValue = GetOriginalValue(navigationEntry);

            var newValue = ChangesProvider.ChangeCalculation.ApplyChange(
                originalValue,
                change);

            if (!navigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
                await navigationEntry.LoadAsync();

            await MaybeUpdateNavigation(navigationEntry, newValue, updateChanges, ControlledMembers, ReuseKeySelector);
        }
        return updateChanges;
    }

    public override async Task FixAsync(object entity, DbContext dbContext)
    {
        var entityEntry = dbContext.Entry(entity);
        var navigationEntry = entityEntry.Navigation(navigationBase);

        if (!navigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
            await navigationEntry.LoadAsync();

        var newValue = _compiledExpression((TEntity)entity);

        await MaybeUpdateNavigation(navigationEntry, newValue, null, ControlledMembers, ReuseKeySelector);
    }

    private static TProperty GetOriginalValue(NavigationEntry navigationEntry)
    {
        if (navigationEntry.EntityEntry.State == EntityState.Added)
            return default!;

        return (TProperty)navigationEntry.GetOriginalValue()!;
    }

    protected override Expression CreateIsValueConsistentExpression(
        Expression computedValue,
        Expression storedValue)
    {
        if (navigationBase.IsCollection)
        {
            var body = ChangeExpressionConditionals(
                computedValue,
                computedValue =>
                {
                    // Unwrap unnecessary stuff
                    while (true)
                    {
                        if (computedValue is UnaryExpression unaryExpression
                            && unaryExpression.NodeType == ExpressionType.Convert)
                        {
                            computedValue = unaryExpression.Operand;
                        }
                        else if (computedValue is MethodCallExpression methodCallExpression
                            && methodCallExpression.Method.DeclaringType == typeof(Enumerable)
                            && (
                                methodCallExpression.Method.Name == nameof(Enumerable.ToList)
                                || methodCallExpression.Method.Name == nameof(Enumerable.ToArray)
                                || methodCallExpression.Method.Name == nameof(Enumerable.AsEnumerable)))
                        {
                            computedValue = methodCallExpression.Arguments[0];
                        }
                        else
                        {
                            break;
                        }
                    }

                    var elementType = navigationBase.TargetEntityType.ClrType;
                    var computedItemParamater = Expression.Parameter(elementType, "c");
                    var storedItemParameter = Expression.Parameter(elementType, "s");

                    Expression storedAndComputedItemMatches = Expression.Equal(Expression.Constant(1), Expression.Constant(1));
                    foreach (var controlledMember in controlledMembers)
                    {
                        storedAndComputedItemMatches = Expression.AndAlso(
                            storedAndComputedItemMatches,
                            Expression.Call(
                                typeof(object), nameof(object.Equals), [],
                                Expression.Convert(
                                    CreateEFPropertyExpression(computedItemParamater, controlledMember),
                                    typeof(object)),
                                Expression.Convert(
                                    CreateEFPropertyExpression(storedItemParameter, controlledMember),
                                    typeof(object))
                            )
                        );
                    }

                    var quantityMatches = Expression.Equal(
                        Expression.Call(
                            typeof(Enumerable), nameof(Enumerable.Count), [elementType],
                            computedValue
                        ),
                        Expression.Call(
                            typeof(Enumerable), nameof(Enumerable.Count), [elementType],
                            storedValue
                        )
                    );

                    var allValuesAreStored = Expression.Not(Expression.Call(
                        typeof(Enumerable), nameof(Enumerable.Any), [storedItemParameter.Type],
                        storedValue,
                        Expression.Lambda(
                            Expression.Not(
                                Expression.Call(
                                    typeof(Enumerable), nameof(Enumerable.Any), [elementType],
                                    computedValue,
                                    Expression.Lambda(
                                        storedAndComputedItemMatches,
                                        computedItemParamater
                                    )
                                )
                            ),
                            storedItemParameter
                        )
                    ));

                    return Expression.And(
                        quantityMatches,
                        allValuesAreStored
                    );
                }
            );

            return body;
        }

        if (navigationBase is INavigation navigation && !navigation.IsCollection)
        {
            var principalKeyProperty = navigation.ForeignKey.PrincipalKey.Properties[0];
            // Compare keys of references
            computedValue = AddPropertyAccess(computedValue, principalKeyProperty);
            storedValue = AddPropertyAccess(storedValue, principalKeyProperty);
        }

        return Expression.Call(
            typeof(object), nameof(object.Equals), [],
            Expression.Convert(computedValue, typeof(object)),
            Expression.Convert(storedValue, typeof(object))
        );
    }

    private static Expression AddPropertyAccess(Expression expression, IProperty property)
    {
        return ChangeExpressionConditionals(expression, (expression) =>
        {
            if (expression is ConstantExpression c && c.Value is null)
            {
                return Expression.Constant(null, MakeNullable(property.ClrType));
            }
            else
            {
                return MakeNullable(CreateEFPropertyExpression(expression, property));
            }
        });
    }

    protected static Expression ChangeExpressionConditionals(
        Expression expression,
        Func<Expression, Expression> change)
    {
        if (expression is ConditionalExpression cond)
        {
            return Expression.Condition(
                cond.Test,
                ChangeExpressionConditionals(cond.IfTrue, change),
                ChangeExpressionConditionals(cond.IfFalse, change)
            );
        }
        else
        {
            return change(expression);
        }
    }

    private static Expression MakeNullable(Expression expression)
    {
        if (IsNullable(expression.Type))
        {
            return expression;
        }
        return Expression.Convert(expression, MakeNullable(expression.Type));
    }

    private static Type MakeNullable(Type type)
    {
        return IsNullable(type)
            ? type
            : typeof(Nullable<>).MakeGenericType(type);
    }

    private static bool IsNullable(Type type)
    {
        return type.IsClass || Nullable.GetUnderlyingType(type) is not null;
    }
}
