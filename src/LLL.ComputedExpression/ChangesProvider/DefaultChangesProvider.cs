
namespace LLL.ComputedExpression.ChangesProvider;

public class DefaultChangesProvider(
    IAffectedEntitiesProvider affectedEntitiesProvider,
    Delegate originalValueGetter,
    Delegate currentValueGetter,
    IEntityActionProvider entityActionProvider
) : IChangesProvider
{
    public async Task<IDictionary<object, (object?, object?)>> GetChangesAsync(object input)
    {
        var affectedEntities = await affectedEntitiesProvider.GetAffectedEntitiesAsync(input);

        return affectedEntities.ToDictionary(e => e, e =>
        {
            var originalValue = entityActionProvider.GetEntityAction(input, e) == EntityAction.Create
                ? default
                : originalValueGetter.DynamicInvoke(input, e);

            var currentValue = entityActionProvider.GetEntityAction(input, e) == EntityAction.Delete
                ? default
                : currentValueGetter.DynamicInvoke(input, e);

            return (originalValue, currentValue);
        });
    }
}