using System.Linq.Expressions;
using LLL.AutoCompute.EntityContexts;
using LLL.AutoCompute.EntityContextTransformers;

namespace LLL.AutoCompute.EntityContextPropagators;

public class LinqMethodsEntityContextPropagator(Lazy<IObservedEntityTypeResolver?> entityTypeResolver)
    : IEntityContextPropagator
{
    public void PropagateEntityContext(
        Expression node,
        IComputedExpressionAnalysis analysis)
    {
        if (node is MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType == typeof(Enumerable) || methodCallExpression.Method.DeclaringType == typeof(Queryable))
            {
                foreach (var arg in methodCallExpression.Arguments.Skip(1))
                {
                    if (GetLambda(arg) is LambdaExpression lambda)
                    {
                        foreach (var param in lambda.Parameters)
                        {
                            analysis.PropagateEntityContext(
                                methodCallExpression.Arguments[0],
                                EntityContextKeys.Element,
                                param,
                                EntityContextKeys.None,
                                new ScopedEntityContextTransformer()
                            );
                        }
                    }
                }

                switch (methodCallExpression.Method.Name)
                {
                    case nameof(Enumerable.All):
                    case nameof(Enumerable.Any):
                    case nameof(Enumerable.Contains):
                        analysis.AddAction(() =>
                        {
                            var entityContext = analysis.ResolveEntityContext(methodCallExpression.Arguments[0], EntityContextKeys.Element);
                            entityContext.MarkNavigationToLoadAll();
                        });
                        break;

                    case nameof(Enumerable.AsEnumerable):
                    case nameof(Enumerable.Cast):
                    case nameof(Enumerable.DefaultIfEmpty):
                    case nameof(Enumerable.OfType):
                    case nameof(Enumerable.Order):
                    case nameof(Enumerable.OrderBy):
                    case nameof(Enumerable.OrderByDescending):
                    case nameof(Enumerable.Reverse):
                    case nameof(Enumerable.Skip):
                    case nameof(Enumerable.SkipLast):
                    case nameof(Enumerable.SkipWhile):
                    case nameof(Enumerable.Take):
                    case nameof(Enumerable.TakeLast):
                    case nameof(Enumerable.TakeWhile):
                    case nameof(Enumerable.ThenBy):
                    case nameof(Enumerable.ThenByDescending):
                    case nameof(Enumerable.ToArray):
                    case nameof(Enumerable.ToList):
                    case nameof(Enumerable.ToHashSet):
                        analysis.PropagateEntityContext(
                            methodCallExpression.Arguments[0],
                            EntityContextKeys.Element,
                            node,
                            EntityContextKeys.Element
                        );
                        break;

                    case nameof(Enumerable.Distinct):
                    case nameof(Enumerable.DistinctBy):
                        analysis.PropagateEntityContext(
                            methodCallExpression.Arguments[0],
                            EntityContextKeys.Element,
                            node,
                            EntityContextKeys.Element,
                            new DistinctEntityContextTransformer()
                        );
                        break;

                    case nameof(Enumerable.Where):
                        {
                            analysis.PropagateEntityContext(
                                methodCallExpression.Arguments[0],
                                EntityContextKeys.Element,
                                node,
                                EntityContextKeys.Element
                            );
                        }
                        break;

                    case nameof(Enumerable.ElementAt):
                    case nameof(Enumerable.ElementAtOrDefault):
                    case nameof(Enumerable.First):
                    case nameof(Enumerable.FirstOrDefault):
                    case nameof(Enumerable.Last):
                    case nameof(Enumerable.LastOrDefault):
                    case nameof(Enumerable.Single):
                    case nameof(Enumerable.SingleOrDefault):
                        analysis.PropagateEntityContext(
                            methodCallExpression.Arguments[0],
                            EntityContextKeys.Element,
                            node,
                            EntityContextKeys.None
                        );
                        break;

                    case nameof(Enumerable.Append):
                        analysis.PropagateEntityContext(
                            [
                                (methodCallExpression.Arguments[0], EntityContextKeys.Element),
                                (methodCallExpression.Arguments[1], EntityContextKeys.None),
                            ],
                            node,
                            EntityContextKeys.Element
                        );
                        break;

                    case nameof(Enumerable.Concat):
                    case nameof(Enumerable.Except):
                    case nameof(Enumerable.ExceptBy):
                    case nameof(Enumerable.Intersect):
                    case nameof(Enumerable.IntersectBy):
                        analysis.PropagateEntityContext(
                            [
                                (methodCallExpression.Arguments[0], EntityContextKeys.Element),
                                (methodCallExpression.Arguments[1], EntityContextKeys.Element),
                            ],
                            node,
                            EntityContextKeys.Element
                        );
                        break;

                    case nameof(Enumerable.ToDictionary):
                        {
                            if (methodCallExpression.Arguments is [_, var keySelector, ..]
                                && GetLambda(keySelector) is LambdaExpression lambda)
                            {
                                analysis.PropagateEntityContext(
                                    lambda.Body,
                                    EntityContextKeys.None,
                                    node,
                                    EntityContextKeys.Element + EntityContextKeys.Key
                                );
                            }

                            if (methodCallExpression.Arguments is [_, _, var valueSelector, ..]
                                && GetLambda(valueSelector) is LambdaExpression valueLambda)
                            {
                                analysis.PropagateEntityContext(
                                    valueLambda.Body,
                                    EntityContextKeys.Element,
                                    node,
                                    EntityContextKeys.Element + EntityContextKeys.Value
                                );
                            }
                            else if (methodCallExpression.Arguments is [var source, ..])
                            {
                                analysis.PropagateEntityContext(
                                    source,
                                    EntityContextKeys.Element,
                                    node,
                                    EntityContextKeys.Element + EntityContextKeys.Value
                                );
                            }
                        }
                        break;

                    case nameof(Enumerable.GroupBy):
                    case nameof(Enumerable.ToLookup):
                        {
                            if (methodCallExpression.Arguments is [_, var keySelector, ..]
                                && GetLambda(keySelector) is LambdaExpression lambda)
                            {
                                analysis.PropagateEntityContext(
                                    lambda.Body,
                                    EntityContextKeys.None,
                                    node,
                                    EntityContextKeys.Element + EntityContextKeys.Key
                                );
                            }

                            if (methodCallExpression.Arguments is [_, _, var valueSelector, ..]
                                && GetLambda(valueSelector) is LambdaExpression valueLambda)
                            {
                                analysis.PropagateEntityContext(
                                    valueLambda.Body,
                                    EntityContextKeys.None,
                                    node,
                                    EntityContextKeys.Element + EntityContextKeys.Element
                                );
                            }
                            else if (methodCallExpression.Arguments is [var source, ..])
                            {
                                analysis.PropagateEntityContext(
                                    source,
                                    EntityContextKeys.Element,
                                    node,
                                    EntityContextKeys.Element + EntityContextKeys.Element
                                );
                            }
                        }
                        break;

                    case nameof(Enumerable.Select):
                        {
                            if (methodCallExpression.Arguments is [_, var selector, ..]
                                && GetLambda(selector) is LambdaExpression selectorLambda)
                            {
                                analysis.PropagateEntityContext(
                                    selectorLambda.Body,
                                    EntityContextKeys.None,
                                    node,
                                    EntityContextKeys.Element
                                );
                            }
                        }
                        break;

                    case nameof(Enumerable.SelectMany):
                        {
                            if (methodCallExpression.Arguments is [_, var selector, ..]
                                && GetLambda(selector) is LambdaExpression selectorLambda)
                            {
                                analysis.PropagateEntityContext(
                                    selectorLambda.Body,
                                    EntityContextKeys.Element,
                                    node,
                                    EntityContextKeys.Element
                                );
                            }
                        }
                        break;

                    case nameof(Enumerable.Empty):
                        var elementType = methodCallExpression.Method.GetGenericArguments().First();
                        var observedEntityType = entityTypeResolver.Value?.Resolve(elementType);
                        if (observedEntityType is not null)
                        {
                            analysis.AddContext(
                                node,
                                EntityContextKeys.Element,
                                new RootEntityContext(node, observedEntityType)
                            );
                        }
                        break;
                }
            }
        }
    }

    private static LambdaExpression? GetLambda(Expression expression)
    {
        return expression switch
        {
            LambdaExpression lambda => lambda,
            ConstantExpression { Value: LambdaExpression lambda } => lambda,
            _ => null
        };
    }
}