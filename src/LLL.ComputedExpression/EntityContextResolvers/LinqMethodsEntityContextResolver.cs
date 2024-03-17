using System.Linq.Expressions;
using L3.Computed.EntityContexts;

namespace L3.Computed;

public class LinqMethodsEntityContextResolver : IEntityContextResolver
{
    public IEntityContext? ResolveEntityContext(
        Expression node,
        IComputedExpressionAnalysis analysis,
        string key)
    {
        if (node is MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType == typeof(Enumerable) || methodCallExpression.Method.DeclaringType == typeof(Queryable))
            {
                switch ((methodCallExpression.Method.Name, key))
                {
                    case (nameof(Enumerable.AsEnumerable), EntityContextKeys.Element):
                    case (nameof(Enumerable.Cast), EntityContextKeys.Element):
                    case (nameof(Enumerable.DefaultIfEmpty), EntityContextKeys.Element):
                    case (nameof(Enumerable.Distinct), EntityContextKeys.Element):
                    case (nameof(Enumerable.DistinctBy), EntityContextKeys.Element):
                    case (nameof(Enumerable.ElementAt), EntityContextKeys.None):
                    case (nameof(Enumerable.ElementAtOrDefault), EntityContextKeys.None):
                    case (nameof(Enumerable.First), EntityContextKeys.None):
                    case (nameof(Enumerable.FirstOrDefault), EntityContextKeys.None):
                    case (nameof(Enumerable.Last), EntityContextKeys.None):
                    case (nameof(Enumerable.LastOrDefault), EntityContextKeys.None):
                    case (nameof(Enumerable.OfType), EntityContextKeys.Element):
                    case (/*nameof(Enumerable.Order)*/ "Order", EntityContextKeys.Element):
                    case (nameof(Enumerable.OrderBy), EntityContextKeys.Element):
                    case (nameof(Enumerable.OrderByDescending), EntityContextKeys.Element):
                    case (nameof(Enumerable.Reverse), EntityContextKeys.Element):
                    case (nameof(Enumerable.Single), EntityContextKeys.None):
                    case (nameof(Enumerable.SingleOrDefault), EntityContextKeys.None):
                    case (nameof(Enumerable.Skip), EntityContextKeys.Element):
                    case (nameof(Enumerable.SkipLast), EntityContextKeys.Element):
                    case (nameof(Enumerable.SkipWhile), EntityContextKeys.Element):
                    case (nameof(Enumerable.Take), EntityContextKeys.Element):
                    case (nameof(Enumerable.TakeLast), EntityContextKeys.Element):
                    case (nameof(Enumerable.TakeWhile), EntityContextKeys.Element):
                    case (nameof(Enumerable.ThenBy), EntityContextKeys.Element):
                    case (nameof(Enumerable.ThenByDescending), EntityContextKeys.Element):
                    case (nameof(Enumerable.ToArray), EntityContextKeys.Element):
                    case (nameof(Enumerable.ToList), EntityContextKeys.Element):
                    case (nameof(Enumerable.ToHashSet), EntityContextKeys.Element):
                    case (nameof(Enumerable.Where), EntityContextKeys.Element):
                        return analysis.ResolveEntityContext(methodCallExpression.Arguments[0], EntityContextKeys.Element);

                    case (nameof(Enumerable.Append), EntityContextKeys.Element):
                        return new CompositeEntityContext(
                            analysis.ResolveEntityContext(methodCallExpression.Arguments[0], EntityContextKeys.Element),
                            analysis.ResolveEntityContext(methodCallExpression.Arguments[1], EntityContextKeys.None)
                        );

                    case (nameof(Enumerable.Concat), EntityContextKeys.Element):
                    case (nameof(Enumerable.Except), EntityContextKeys.Element):
                    case (nameof(Enumerable.ExceptBy), EntityContextKeys.Element):
                    case (nameof(Enumerable.Intersect), EntityContextKeys.Element):
                    case (nameof(Enumerable.IntersectBy), EntityContextKeys.Element):
                        return new CompositeEntityContext(
                            analysis.ResolveEntityContext(methodCallExpression.Arguments[0], EntityContextKeys.Element),
                            analysis.ResolveEntityContext(methodCallExpression.Arguments[1], EntityContextKeys.Element)
                        );

                    case (nameof(Enumerable.ToDictionary), EntityContextKeys.Element + EntityContextKeys.Key):
                        switch (methodCallExpression.Arguments)
                        {
                            case [_, LambdaExpression lambda, ..]:
                                return analysis.ResolveEntityContext(lambda.Body, EntityContextKeys.None);
                            case [_, ConstantExpression { Value: LambdaExpression lambda }, ..]:
                                return analysis.ResolveEntityContext(lambda.Body, EntityContextKeys.None);
                        }
                        break;

                    case (nameof(Enumerable.ToDictionary), EntityContextKeys.Element + EntityContextKeys.Value):
                        switch (methodCallExpression.Arguments)
                        {
                            case [_, _, LambdaExpression lambda, ..]:
                                return analysis.ResolveEntityContext(lambda.Body, EntityContextKeys.Element);
                            case [_, _, ConstantExpression { Value: LambdaExpression lambda }, ..]:
                                return analysis.ResolveEntityContext(lambda.Body, EntityContextKeys.Element);
                            case [var source, ..]:
                                return analysis.ResolveEntityContext(source, EntityContextKeys.Element);
                        }
                        break;

                    case (nameof(Enumerable.ToLookup), EntityContextKeys.Element + EntityContextKeys.Key):
                        switch (methodCallExpression.Arguments)
                        {
                            case [_, LambdaExpression lambda, ..]:
                                return analysis.ResolveEntityContext(lambda.Body, EntityContextKeys.None);
                            case [_, ConstantExpression { Value: LambdaExpression lambda }, ..]:
                                return analysis.ResolveEntityContext(lambda.Body, EntityContextKeys.None);
                        }
                        break;

                    case (nameof(Enumerable.ToLookup), EntityContextKeys.Element + EntityContextKeys.Element):
                        switch (methodCallExpression.Arguments)
                        {
                            case [_, _, LambdaExpression lambda, ..]:
                                return analysis.ResolveEntityContext(lambda.Body, EntityContextKeys.None);
                            case [_, _, ConstantExpression { Value: LambdaExpression lambda }, ..]:
                                return analysis.ResolveEntityContext(lambda.Body, EntityContextKeys.None);
                            case [var source, ..]:
                                return analysis.ResolveEntityContext(source, EntityContextKeys.Element);
                        }
                        break;

                    case (nameof(Enumerable.Select), EntityContextKeys.Element):
                        switch (methodCallExpression.Arguments)
                        {
                            case [_, LambdaExpression lambda, ..]:
                                return analysis.ResolveEntityContext(lambda.Body, EntityContextKeys.None);
                            case [_, ConstantExpression { Value: LambdaExpression lambda }, ..]:
                                return analysis.ResolveEntityContext(lambda.Body, EntityContextKeys.None);
                        }
                        break;

                    case (nameof(Enumerable.SelectMany), EntityContextKeys.Element):
                        switch (methodCallExpression.Arguments)
                        {
                            case [_, LambdaExpression lambda, ..]:
                                return analysis.ResolveEntityContext(lambda.Body, EntityContextKeys.Element);
                            case [_, ConstantExpression { Value: LambdaExpression lambda }, ..]:
                                return analysis.ResolveEntityContext(lambda.Body, EntityContextKeys.Element);
                        }
                        break;
                }
            }
        }

        return null;
    }
}
