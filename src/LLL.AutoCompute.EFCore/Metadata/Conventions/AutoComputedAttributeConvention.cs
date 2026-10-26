using System.Linq.Expressions;
using System.Reflection;
using LLL.AutoCompute.EFCore.Infrastructure;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace LLL.AutoCompute.EFCore.Metadata.Conventions;

class AutoComputedAttributeConvention : IModelFinalizingConvention
{
    private static readonly MethodInfo RegisterMethod = typeof(AutoComputedAttributeConvention)
        .GetMethod(nameof(Register), BindingFlags.NonPublic | BindingFlags.Static)!;

    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes().OrderBy(e => e.GetAllBaseTypes().Count()).ToArray())
        {
            foreach (var propertyInfo in entityType.ClrType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (!propertyInfo.IsDefined(typeof(AutoComputedAttribute), inherit: false)
                    || entityType.FindIgnoredConfigurationSource(propertyInfo.Name) is not null
                    || entityType.BaseType?.FindProperty(propertyInfo.Name) is not null)
                    continue;

                var factory = propertyInfo.DeclaringType!.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Select(m => (Method: m, Metadata: m.GetCustomAttribute<AutoComputedExpressionAttribute>()))
                    .SingleOrDefault(m => m.Metadata?.PropertyName == propertyInfo.Name);
                if (factory.Metadata is null)
                    throw new InvalidOperationException($"AutoComputed property '{entityType.ClrType.Name}.{propertyInfo.Name}' has no generated expression. " +
                        "Reference LLL.AutoCompute.EFCore in the project defining the partial entity and enable its source generator.");

                var field = propertyInfo.DeclaringType.GetField(factory.Metadata.FieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)!;
                var builder = entityType.Builder.Property(propertyInfo, fromDataAnnotation: true)
                    ?? throw new InvalidOperationException($"Cannot map AutoComputed property '{entityType.ClrType.Name}.{propertyInfo.Name}'.");

                // Every EF read and write must use storage, including change tracking after materialization.
                if (builder.HasField(field, fromDataAnnotation: true) is null
                    || builder.UsePropertyAccessMode(PropertyAccessMode.Field, fromDataAnnotation: true) is null)
                    throw new InvalidOperationException($"AutoComputed property '{entityType.ClrType.Name}.{propertyInfo.Name}' requires its generated backing field and PropertyAccessMode.Field.");

                builder.HasAnnotation(AutoComputeAnnotationNames.Declarative, true, fromDataAnnotation: true);
                if (((IReadOnlyProperty)builder.Metadata).GetComputedFactory() is not null)
                    continue;

                var expression = (LambdaExpression)factory.Method.Invoke(null, null)!;
                RegisterMethod.MakeGenericMethod(entityType.ClrType, propertyInfo.PropertyType)
                    .Invoke(null, [builder, expression]);
            }
        }
    }

    private static void Register<TEntity, TProperty>(IConventionPropertyBuilder builder, LambdaExpression expression)
        where TEntity : class
    {
        var parameter = Expression.Parameter(typeof(TEntity), expression.Parameters[0].Name);
        var body = new ReplaceParameter(expression.Parameters[0], parameter).Visit(expression.Body)!;
        var typedExpression = Expression.Lambda<Func<TEntity, TProperty>>(body, parameter);
        builder.HasAnnotation(AutoComputeAnnotationNames.MemberFactory,
            ComputedMemberFactory.CreateComputedPropertyFactory(typedExpression,
                ChangeCalculatorFactory<TProperty>.Instance.CurrentValue()), fromDataAnnotation: true);
        builder.HasAnnotation(AutoComputeAnnotationNames.RawExpression, typedExpression, fromDataAnnotation: true);
    }

    private sealed class ReplaceParameter(ParameterExpression original, ParameterExpression replacement) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == original ? replacement : node;
    }
}
