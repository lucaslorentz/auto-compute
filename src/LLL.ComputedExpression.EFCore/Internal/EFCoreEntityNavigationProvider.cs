using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace L3.Computed.EFCore.Internal;

public class EFCoreEntityNavigationProvider(IModel model)
    : IEntityNavigationProvider<EFCoreEntityNavigationProvider.IInput>
{
    public interface IInput
    {
        DbContext DbContext { get; }
    }

    public IEntityNavigation<IInput>? GetEntityNavigation(Expression node)
    {
        if (node is MemberExpression memberExpression
            && memberExpression.Expression is not null)
        {
            var type = memberExpression.Expression.Type;
            var entityType = model.FindEntityType(type);
            var navigation = entityType?.FindNavigation(memberExpression.Member);
            if (navigation != null)
                return new EntityNavigation(memberExpression.Expression, navigation);
        }

        return null;
    }

    class EntityNavigation(
        Expression sourceExpression,
        INavigation navigation
    ) : IEntityNavigation<IInput>
    {
        public bool IsCollection => navigation.IsCollection;
        public Expression SourceExpression => sourceExpression;
        public Type TargetType => navigation.TargetEntityType.ClrType;

        public IEntityNavigationLoader<IInput> GetInverseLoader()
        {
            var inverseNavigation = navigation.Inverse
                ?? throw new InvalidOperationException($"No inverse for navigation '{navigation.DeclaringType.ShortName()}.{navigation.Name}'");

            if (inverseNavigation.IsCollection)
            {
                return new CollectionEntityNavigationLoader(inverseNavigation);
            }
            else if (!inverseNavigation.IsCollection)
            {
                return new ReferenceEntityNavigationLoader(inverseNavigation);
            }

            throw new NotImplementedException("Unsupported navigation");
        }
    }

    class CollectionEntityNavigationLoader(INavigation inverseNavigation)
        : IEntityNavigationLoader<IInput>
    {
        public Type TargetType => inverseNavigation.DeclaringEntityType.ClrType;

        public async Task<IEnumerable<object>> LoadAsync(IInput input, IEnumerable<object> targetEntities)
        {
            var sourceEntities = new HashSet<object>();
            foreach (var targetEntity in targetEntities)
            {
                var navigationEntry = input.DbContext.Entry(targetEntity).Navigation(inverseNavigation);
                if (!navigationEntry.IsLoaded)
                {
                    await navigationEntry.LoadAsync();
                }
                if (navigationEntry.CurrentValue is IEnumerable enumerable)
                {
                    foreach (var sourceEntity in enumerable)
                    {
                        sourceEntities.Add(sourceEntity);
                    }
                }
            }
            return sourceEntities;
        }
    }


    class ReferenceEntityNavigationLoader(INavigation inverseNavigation)
        : IEntityNavigationLoader<IInput>
    {
        public Type TargetType => inverseNavigation.DeclaringEntityType.ClrType;

        public async Task<IEnumerable<object>> LoadAsync(IInput input, IEnumerable<object> targetEntities)
        {
            var sourceEntities = new HashSet<object>();
            foreach (var targetEntity in targetEntities)
            {
                var navigationEntry = input.DbContext.Entry(targetEntity).Navigation(inverseNavigation);
                if (!navigationEntry.IsLoaded)
                {
                    await navigationEntry.LoadAsync();
                }
                if (navigationEntry.CurrentValue is not null)
                {
                    sourceEntities.Add(navigationEntry.CurrentValue);
                }
            }
            return sourceEntities;
        }
    }
}
