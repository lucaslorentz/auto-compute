using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;

namespace LLL.Computed.EntityContextPropagators;

public class LinqMethodsEntityContextPropagator
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
                                EntityContextKeys.None
                            );
                        }
                    }
                }

                switch (methodCallExpression.Method.Name)
                {
                    case nameof(Enumerable.AsEnumerable):
                    case nameof(Enumerable.Cast):
                    case nameof(Enumerable.DefaultIfEmpty):
                    case nameof(Enumerable.Distinct):
                    case nameof(Enumerable.DistinctBy):
                    case nameof(Enumerable.OfType):
                    case /*nameof(Enumerable.Order)*/ "Order":
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
                    case nameof(Enumerable.Where):
                        analysis.PropagateEntityContext(
                            methodCallExpression.Arguments[0],
                            EntityContextKeys.Element,
                            node,
                            EntityContextKeys.Element
                        );
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
                                    EntityContextKeys.None,
                                    node,
                                    EntityContextKeys.Element
                                );
                            }
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