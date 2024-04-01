using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.ComputedExpression.EFCore.Internal;

public class EFCoreMemberAccessLocator(IModel model) :
    IEntityNavigationAccessLocator<IEFCoreComputedInput>,
    IEntityPropertyAccessLocator<IEFCoreComputedInput>
{
    public IEntityMemberAccess<IEntityNavigation>? GetEntityNavigationAccess(Expression node)
    {
        if (node is MemberExpression memberExpression
            && memberExpression.Expression is not null)
        {
            var type = memberExpression.Expression.Type;
            var entityType = model.FindEntityType(type);
            var navigation = entityType?.FindNavigation(memberExpression.Member);
            if (navigation != null)
                return EntityMemberAccess.Create(memberExpression.Expression, new EFCoreEntityNavigation(navigation));
        }

        return null;
    }

    public IEntityMemberAccess<IEntityProperty>? GetEntityPropertyAccess(Expression node)
    {
        if (node is MemberExpression memberExpression
            && memberExpression.Expression is not null)
        {
            var type = memberExpression.Expression.Type;
            var entityType = model.FindEntityType(type);
            var property = entityType?.FindProperty(memberExpression.Member);
            if (property is not null)
                return EntityMemberAccess.Create(memberExpression.Expression, new EFCoreEntityProperty(property));
        }

        return null;
    }
}
