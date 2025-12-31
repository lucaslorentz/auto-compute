
using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Metadata.Internal.ExpressionVisitors;

namespace LLL.AutoCompute.EFCore;

internal static class ExpressionExtensions
{
    public static Expression UnwrapLambda(
        this LambdaExpression lambda,
        Expression[] parameters)
    {
        if (lambda.Parameters.Count != parameters.Length)
            throw new ArgumentException($"Expected {lambda.Parameters.Count} parameters but got {parameters.Length}");

        var replacements = lambda.Parameters.OfType<Expression>().Zip(parameters).ToDictionary();

        return new ReplaceExpressionsVisitor(replacements)
            .Visit(lambda.Body);
    }
}