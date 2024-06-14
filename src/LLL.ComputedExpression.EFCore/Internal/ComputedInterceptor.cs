using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LLL.ComputedExpression.EFCore;

public class ComputedInterceptor : ISaveChangesInterceptor
{
    public async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await eventData.Context!.UpdateComputedsAsync();

        return result;
    }

    public InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        eventData.Context!.UpdateComputedsAsync().GetAwaiter().GetResult();

        return result;
    }
}
