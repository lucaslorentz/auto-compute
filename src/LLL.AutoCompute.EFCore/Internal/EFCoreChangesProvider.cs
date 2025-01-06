using LLL.AutoCompute.EFCore;
using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore;

namespace LLL.AutoCompute.ChangesProviders;

public class EFCoreChangesProvider<TEntity, TChange>(
    IUnboundChangesProvider<IEFCoreComputedInput, TEntity, TChange> unboundChangesProvider,
    DbContext dbContext)
    where TEntity : class
{
    private readonly ChangeMemory<TEntity, TChange> _memory = new();

    public async Task<IReadOnlyDictionary<TEntity, TChange>> GetChangesAsync()
    {
        var observedMembers = unboundChangesProvider.EntityContext.GetAllObservedMembers()
            .OfType<EFCoreObservedMember>()
            .ToArray();

        var changesToProcess = new EFCoreChangeset();

        foreach (var observedMember in observedMembers)
            await observedMember.CollectChangesAsync(dbContext, changesToProcess);

        var input = new EFCoreComputedInput(dbContext, changesToProcess);

        return await unboundChangesProvider.GetChangesAsync(input, _memory);
    }
}