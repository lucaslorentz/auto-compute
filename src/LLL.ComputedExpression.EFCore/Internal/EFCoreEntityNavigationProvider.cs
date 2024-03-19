using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.Computed.EFCore.Internal;

public class EFCoreEntityNavigationProvider(IModel model)
    : IEntityNavigationProvider<EFCoreEntityNavigationProvider.IInput>
{
    public interface IInput
    {
        DbContext DbContext { get; }
    }

    public IExpressionMatch<IEntityNavigation<IInput>>? GetEntityNavigation(Expression node)
    {
        if (node is MemberExpression memberExpression
            && memberExpression.Expression is not null)
        {
            var type = memberExpression.Expression.Type;
            var entityType = model.FindEntityType(type);
            var navigation = entityType?.FindNavigation(memberExpression.Member);
            if (navigation != null)
                return ExpressionMatch.Create(memberExpression.Expression, new EntityNavigation(navigation));
        }

        return null;
    }

    class EntityNavigation(
        INavigation navigation
    ) : IEntityNavigation<IInput>
    {
        public bool IsCollection => navigation.IsCollection;
        public Type TargetType => navigation.TargetEntityType.ClrType;

        public IEntityNavigation<IInput> GetInverse()
        {
            var inverse = navigation.Inverse
                ?? throw new InvalidOperationException($"No inverse for navigation '{navigation.DeclaringType.ShortName()}.{navigation.Name}'");

            return new EntityNavigation(inverse);
        }

        public async Task<IEnumerable<object>> LoadAsync(IInput input, IEnumerable<object> targetEntities)
        {
            var sourceEntities = new HashSet<object>();
            foreach (var targetEntity in targetEntities)
            {
                var navigationEntry = input.DbContext.Entry(targetEntity).Navigation(navigation);
                if (!navigationEntry.IsLoaded)
                {
                    await navigationEntry.LoadAsync();
                }
                if (navigation.IsCollection)
                {
                    if (navigationEntry.CurrentValue is IEnumerable enumerable)
                    {
                        foreach (var sourceEntity in enumerable)
                        {
                            sourceEntities.Add(sourceEntity);
                        }
                    }
                }
                else
                {
                    if (navigationEntry.CurrentValue is not null)
                    {
                        sourceEntities.Add(navigationEntry.CurrentValue);
                    }
                }
            }
            return sourceEntities;
        }

        public string ToDebugString()
        {
            return $"{navigation.Name}";
        }
    }
}
